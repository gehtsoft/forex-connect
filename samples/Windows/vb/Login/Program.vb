Imports System.Text
Imports System.Threading
Imports System.Runtime.InteropServices
Imports fxcore2

Namespace Login
    Public Class Program
        Public Shared strSessionID As String = ""
        Public Shared strPin As String = ""

        <DllImport("Kernel32")> _
        Private Shared Function SetConsoleCtrlHandler(ByVal handler As EventHandler, ByVal add As Boolean) As Boolean
        End Function
        Public Delegate Function EventHandler(ByVal sig As CtrlType) As Boolean
        Public Shared _handler As EventHandler
        Public Enum CtrlType
            CTRL_C_EVENT = 0
            CTRL_BREAK_EVENT = 1
            CTRL_CLOSE_EVENT = 2
            CTRL_LOGOFF_EVENT = 5
            CTRL_SHUTDOWN_EVENT = 6
        End Enum

        Public Shared Function Handler(ByVal sig As CtrlType) As Boolean
            SetConsoleCtrlHandler(New EventHandler(AddressOf Handler), True)

            Select Case sig
                Case CtrlType.CTRL_C_EVENT, CtrlType.CTRL_BREAK_EVENT, CtrlType.CTRL_LOGOFF_EVENT, CtrlType.CTRL_SHUTDOWN_EVENT, CtrlType.CTRL_CLOSE_EVENT
                    Return True
                Case Else
                    Return False
            End Select
        End Function
        Shared Sub Main(ByVal args() As String)
            If args.Length < 4 Then
                Console.WriteLine("Not Enough Parameters!")
                Console.WriteLine("USAGE: [application].exe [user ID] [password] [URL] [connection] [session ID (if needed)] [pin (if needed)]")
                Console.WriteLine(vbLf & "Press any key to close the program")
                Console.ReadKey()
                Return
            End If

            Dim strUserID As String = args(0)
            Dim strPassword As String = args(1)
            Dim strURL As String = args(2)
            Dim strConnection As String = args(3)

            If args.Length > 4 Then
                strSessionID = args(4)
            End If

            If args.Length > 5 Then
                strPin = args(5)
            End If

            Try
                Dim session As O2GSession = O2GTransport.createSession()
                Dim statusListener As New mySessionStatusListener(session)
                session.subscribeSessionStatus(statusListener)
                session.login(strUserID, strPassword, strURL, strConnection)
                Do While (Not statusListener.Connected) AndAlso Not statusListener.Error
                    Thread.Sleep(50)
                Loop
                If Not statusListener.Error Then
                    session.logout()
                    Do While Not statusListener.Disconnected
                        Thread.Sleep(50)
                    Loop
                End If
                session.unsubscribeSessionStatus(statusListener)
                Console.WriteLine("Done! Press any key to exit")
                Console.ReadKey()
                session.Dispose()
            Catch e As Exception
                Console.WriteLine("Exception: {0}", e.ToString())
            End Try

        End Sub
    End Class
	Public Class mySessionStatusListener
        Implements fxcore2.IO2GSessionStatus
		Private mConnected As Boolean = False
		Private mDiconnected As Boolean = False
		Private mError As Boolean = False
		Private mSession As O2GSession

		Public Sub New(ByVal session As O2GSession)
			mSession = session
		End Sub
		Public ReadOnly Property Connected() As Boolean
			Get
				Return mConnected
			End Get
		End Property
		Public ReadOnly Property Disconnected() As Boolean
			Get
				Return mDiconnected
			End Get
		End Property
		Public ReadOnly Property [Error]() As Boolean
			Get
				Return mError
			End Get
		End Property
        Public Sub onSessionStatusChanged(ByVal status As O2GSessionStatusCode) Implements fxcore2.IO2GSessionStatus.onSessionStatusChanged
            Console.WriteLine("Status: " & status.ToString())
            If status = O2GSessionStatusCode.Connected Then
                mConnected = True
            Else
                mConnected = False
            End If

            If status = O2GSessionStatusCode.Disconnected Then
                mDiconnected = True
            Else
                mDiconnected = False
            End If

            If status = O2GSessionStatusCode.TradingSessionRequested Then
                Dim descs As O2GSessionDescriptorCollection = mSession.getTradingSessionDescriptors()
                Console.WriteLine(vbLf & "Session descriptors")
                Console.WriteLine("id, name, description, requires pin")
                For Each desc As O2GSessionDescriptor In descs
                    Console.WriteLine("'{0}' '{1}' '{2}' {3}", desc.Id, desc.Name, desc.Description, desc.RequiresPin)
                Next desc
                Console.WriteLine()

                If Program.strSessionID = "" Then
                    Console.WriteLine("Argument for trading session ID is missing")
                Else
                    mSession.setTradingSession(Program.strSessionID, Program.strPin)
                End If
            End If

        End Sub

        Public Sub onLoginFailed(ByVal err As String) Implements fxcore2.IO2GSessionStatus.onLoginFailed
            Console.WriteLine("Login error: " & err)
            mError = True
        End Sub
    End Class

End Namespace
