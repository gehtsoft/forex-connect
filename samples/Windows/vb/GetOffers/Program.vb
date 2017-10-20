Imports System.Text
Imports System.Threading
Imports System.Runtime.InteropServices
Imports fxcore2

Namespace GetOffers
    Public Class Program
        Private Shared mSession As O2GSession
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
            Select Case sig
                Case CtrlType.CTRL_C_EVENT, CtrlType.CTRL_BREAK_EVENT, CtrlType.CTRL_LOGOFF_EVENT, CtrlType.CTRL_SHUTDOWN_EVENT, CtrlType.CTRL_CLOSE_EVENT
                    Return True
                Case Else
                    Return False
            End Select
        End Function

        Shared Sub Main(ByVal args() As String)
            SetConsoleCtrlHandler(New EventHandler(AddressOf Handler), True)

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
                mSession = O2GTransport.createSession()

                Dim statusListener As New SessionStatusListener(mSession)
                Dim responseListener As New ResponseListener()
                mSession.subscribeResponse(responseListener)
                mSession.subscribeSessionStatus(statusListener)
                mSession.login(strUserID, strPassword, strURL, strConnection)
                Do While statusListener.Status <> O2GSessionStatusCode.Connected

                Loop
                Thread.Sleep(10000)
                mSession.logout()
                mSession.unsubscribeSessionStatus(statusListener)
                mSession.unsubscribeResponse(responseListener)
                mSession.Dispose()
            Catch e As Exception
                Console.WriteLine("Exception: {0}", e.ToString())
            End Try
        End Sub

        Public Shared Sub printOffers(ByVal response As O2GResponse)
            Dim readerFactory As O2GResponseReaderFactory = mSession.getResponseReaderFactory()
            If readerFactory IsNot Nothing Then
                Dim reader As O2GOffersTableResponseReader = readerFactory.createOffersTableReader(response)
                Dim i As Integer
                For i = 0 To reader.Count - 1
                    Dim row As O2GOfferRow = reader.getRow(i)
                    Console.WriteLine("{0} {1} {2} {3}", row.OfferID, row.Instrument, row.Bid, row.Ask)
                Next i
            End If
        End Sub
    End Class

    Public Class SessionStatusListener
        Implements fxcore2.IO2GSessionStatus
        Private mCode As O2GSessionStatusCode = O2GSessionStatusCode.Unknown
        Private mSession As O2GSession = Nothing

        Public ReadOnly Property Status() As O2GSessionStatusCode
            Get
                Return mCode
            End Get
        End Property

        Public Sub New(ByVal session As O2GSession)
            mSession = session
        End Sub

        Public Sub onSessionStatusChanged(ByVal code As O2GSessionStatusCode) Implements fxcore2.IO2GSessionStatus.onSessionStatusChanged
            mCode = code
            If code = O2GSessionStatusCode.Connected Then
                Dim rules As O2GLoginRules = mSession.getLoginRules()
                If rules.isTableLoadedByDefault(fxcore2.O2GTableType.Offers) Then
                    Program.printOffers(rules.getTableRefreshResponse(fxcore2.O2GTableType.Offers))
                Else
                    Dim requestFactory As O2GRequestFactory = mSession.getRequestFactory()
                    If requestFactory IsNot Nothing Then
                        mSession.sendRequest(requestFactory.createRefreshTableRequest(fxcore2.O2GTableType.Offers))
                    End If
                End If
            ElseIf code = O2GSessionStatusCode.TradingSessionRequested Then
                If Program.strSessionID = "" Then
                    Console.WriteLine("Argument for trading session ID is missing")
                Else
                    mSession.setTradingSession(Program.strSessionID, Program.strPin)
                End If
            End If
        End Sub

        Public Sub onLoginFailed(ByVal [error] As String) Implements fxcore2.IO2GSessionStatus.onLoginFailed
            Console.WriteLine("Login error " & [error])
        End Sub
    End Class

    Friend Class ResponseListener
        Implements fxcore2.IO2GResponseListener
        Public Sub onRequestCompleted(ByVal requestID As String, ByVal response As O2GResponse) Implements fxcore2.IO2GResponseListener.onRequestCompleted
            If response.Type = fxcore2.O2GResponseType.GetOffers Then
                Program.printOffers(response)
            End If
        End Sub

        Public Sub onRequestFailed(ByVal requestID As String, ByVal [error] As String) Implements fxcore2.IO2GResponseListener.onRequestFailed
        End Sub

        Public Sub onTablesUpdates(ByVal response As O2GResponse) Implements fxcore2.IO2GResponseListener.onTablesUpdates
            If response.Type = fxcore2.O2GResponseType.GetOffers OrElse response.Type = fxcore2.O2GResponseType.TablesUpdates Then
                Program.printOffers(response)
            End If
        End Sub
    End Class
End Namespace
