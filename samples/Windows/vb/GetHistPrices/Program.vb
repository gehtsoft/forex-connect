Imports System.Text
Imports System.Threading
Imports System.Runtime.InteropServices
Imports fxcore2

Namespace GetHistPrices
    Public Class Program
        Public Shared strSessionId As String = ""
        Public Shared strPin As String = ""
        Public Shared iMaxBars As Integer = 300

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
                Console.WriteLine("USAGE: [application].exe [user ID] [password] [URL] [connection] [session ID (if needed) or empty string] [pin (if needed) or empty string] [instrument (default ""EUR/USD""] [time frame (default ""m1"")] [datetime ""from"" or empty string for ""from now"" (default)] [datetime ""to"" or empty string for ""to now"" (default)] [max number of bars (default 300)]")
                Console.WriteLine(vbLf & "Press any key to close the program")
                Console.ReadKey()
                Return
            End If
            Dim strUserID As String = args(0)
            Dim strPassword As String = args(1)
            Dim strURL As String = args(2)
            Dim strConnection As String = args(3)
            If args.Length > 4 Then
                strSessionId = args(4)
            End If
            If args.Length > 5 Then
                strPin = args(5)
            End If
            Dim strInstrument As String = "EUR/USD"
            If args.Length > 6 Then
                strInstrument = args(6)
            End If
            Dim strTimeFrame As String = "m1"
            If args.Length > 7 Then
                strTimeFrame = args(7)
            End If
            Dim strDateTimeFrom As String = ""
            If args.Length > 8 Then
                strDateTimeFrom = args(8)
            End If
            Dim dtFrom As Date = Date.FromOADate(0)
            Try
                If strDateTimeFrom <> "" Then
                    dtFrom = Convert.ToDateTime(strDateTimeFrom)
                End If
            Catch
                dtFrom = Date.FromOADate(0)
            End Try
            Dim strDateTimeTo As String = ""
            If args.Length > 9 Then
                strDateTimeTo = args(9)
            End If
            Dim dtTo As Date = Date.FromOADate(0)
            Try
                If strDateTimeTo <> "" Then
                    dtTo = Convert.ToDateTime(strDateTimeTo)
                End If
            Catch
                dtTo = Date.FromOADate(0)
            End Try
            Dim bDateOK As Boolean = CheckDates(dtFrom, dtTo)
            If Not bDateOK Then
                Console.WriteLine(vbLf & "Press any key to close the program")
                Console.ReadKey()
                Return
            End If
            Dim strMaxBars As String = "300"
            If args.Length > 10 Then
                strMaxBars = args(10)
            End If
            Try
                iMaxBars = Convert.ToInt32(strMaxBars)
            Catch
                iMaxBars = 300
            End Try

            Try
                Dim session As O2GSession = O2GTransport.createSession()
                Dim statusListener As New SessionStatusListener(session)

                statusListener.login(strUserID, strPassword, strURL, strConnection)

                Dim responseListener As New ResponseListener(session)
                responseListener.get(strInstrument, strTimeFrame, dtFrom, dtTo)
                Dim reader As O2GMarketDataSnapshotResponseReader = responseListener.Reader
                For i As Integer = 0 To reader.Count - 1
                    Console.WriteLine("{0} {1} {2} {3} {4} {5}", reader.getDate(i), reader.getBidOpen(i), reader.getBidHigh(i), reader.getBidLow(i), reader.getBidClose(i), reader.getVolume(i))
                Next i
                statusListener.logout()
                Console.WriteLine("Done! Press any key to exit")
                Console.ReadKey()
                session.Dispose()
            Catch e As Exception
                Console.WriteLine("Exception: {0}", e.ToString())
            End Try
        End Sub

        Private Shared Function CheckDates(ByVal dtFrom As Date, ByVal dtTo As Date) As Boolean
            Dim bDateOK As Boolean = True
            If dtTo <> Date.FromOADate(0) Then
                If dtTo > Date.Now Then
                    Console.WriteLine("Date and time ""to"" should not be in a future")
                    bDateOK = False
                End If
            End If
            If dtFrom <> Date.FromOADate(0) Then
                If dtFrom > Date.Now Then
                    Console.WriteLine("Date and time ""from"" should not be in a future")
                    bDateOK = False
                End If
            End If
            If dtTo <> Date.FromOADate(0) AndAlso dtFrom <> Date.FromOADate(0) Then
                If dtFrom >= dtTo Then
                    Console.WriteLine("Date ""to"" should be later than date ""from""")
                    bDateOK = False
                End If
            End If
            Return bDateOK
        End Function
    End Class

    Friend Class ResponseListener
        Implements fxcore2.IO2GResponseListener
        Private mSession As O2GSession
        Private mEvent As New Object()
        Private mError As String
        Private mRequest As String
        Private mReader As O2GMarketDataSnapshotResponseReader

        Public ReadOnly Property Reader() As O2GMarketDataSnapshotResponseReader
            Get
                Return mReader
            End Get
        End Property

        Public Sub New(ByVal session As O2GSession)
            mSession = session
        End Sub

        Public Sub [get](ByVal instrument As String, ByVal tf As String, ByVal [from] As Date, ByVal [to] As Date)
            Dim tfo As O2GTimeframe
            Dim factory As O2GRequestFactory = mSession.getRequestFactory()
            Dim timeframes As O2GTimeframeCollection = factory.Timeframes
            tfo = timeframes(tf)
            If tfo Is Nothing Then
                Throw New Exception("ResponseListener: time frame is incorrect")
            End If
            Dim request As O2GRequest = factory.createMarketDataSnapshotRequestInstrument(instrument, tfo, Program.iMaxBars)
            factory.fillMarketDataSnapshotRequestTime(request, [from], [to], False)
            mError = Nothing
            mReader = Nothing
            mRequest = request.RequestID

            mSession.subscribeResponse(Me)

            Try
                mSession.sendRequest(request)

                Do
                    SyncLock mEvent
                        Monitor.Wait(mEvent, 250)
                    End SyncLock
                    If mError Is Nothing AndAlso mReader Is Nothing Then
                        Continue Do
                    End If
                    If mError IsNot Nothing Then
                        Throw New Exception("ResponseListener:" & mError)
                    End If
                    Exit Do
                Loop
            Finally
                mSession.unsubscribeResponse(Me)
            End Try
        End Sub

        Public Sub onRequestCompleted(ByVal request As String, ByVal response As O2GResponse) Implements fxcore2.IO2GResponseListener.onRequestCompleted
            If request = mRequest Then
                If response.Type <> fxcore2.O2GResponseType.MarketDataSnapshot Then
                    mError = "Incorrect response"
                Else
                    Dim factory As O2GResponseReaderFactory = mSession.getResponseReaderFactory()
                    mReader = factory.createMarketDataSnapshotReader(response)

                End If
                SyncLock mEvent
                    Monitor.PulseAll(mEvent)
                End SyncLock
            End If
        End Sub

        Public Sub onRequestFailed(ByVal request As String, ByVal [error] As String) Implements fxcore2.IO2GResponseListener.onRequestFailed
            If request = mRequest Then
                mError = [error]
                Console.WriteLine("Error: " + Chr(34) + [error] + Chr(34) + "; MarketDataSnapshot request failed")
                Console.WriteLine("Press any key to close program")
                Console.ReadKey()
                SyncLock mEvent
                    Monitor.PulseAll(mEvent)
                End SyncLock
            End If
        End Sub

        Public Sub onTablesUpdates(ByVal data As O2GResponse) Implements fxcore2.IO2GResponseListener.onTablesUpdates
        End Sub
    End Class

    Friend Class SessionStatusListener
        Implements fxcore2.IO2GSessionStatus
        Private mSession As O2GSession = Nothing
        Private mEvent As New Object()
        Private mSuccess As Boolean
        Private mError As String

        Public Sub New(ByVal session As O2GSession)
            mSession = session
        End Sub

        Public Sub login(ByVal user As String, ByVal password As String, ByVal url As String, ByVal connection As String)
            mSuccess = False
            mError = Nothing

            mSession.subscribeSessionStatus(Me)

            Try
                mSession.login(user, password, url, connection)
                SyncLock mEvent
                    Monitor.Wait(mEvent)
                End SyncLock
                If Not mSuccess Then
                    Throw New Exception("Login:" & mError)
                End If
            Finally
                mSession.unsubscribeSessionStatus(Me)
            End Try
        End Sub

        Public Sub logout()
            mSession.subscribeSessionStatus(Me)
            Try
                mSuccess = False
                mSession.logout()

                Do
                    SyncLock mEvent
                        Monitor.Wait(mEvent, 250)
                    End SyncLock
                    If mSuccess Then
                        Exit Do
                    End If
                Loop
            Finally
                mSession.unsubscribeSessionStatus(Me)
            End Try

        End Sub

        Public Sub onLoginFailed(ByVal [error] As String) Implements fxcore2.IO2GSessionStatus.onLoginFailed
            mError = [error]
            SyncLock mEvent
                Monitor.PulseAll(mEvent)
            End SyncLock
        End Sub

        Public Sub onSessionStatusChanged(ByVal status As O2GSessionStatusCode) Implements fxcore2.IO2GSessionStatus.onSessionStatusChanged
            If status = O2GSessionStatusCode.TradingSessionRequested Then
                If Program.strSessionId = "" AndAlso Program.strPin = "" Then
                    Console.WriteLine("Argument for trading session ID is missing")
                Else
                    mSession.setTradingSession(Program.strSessionId, Program.strPin)
                End If
            ElseIf status = O2GSessionStatusCode.Connected Then
                mSuccess = True
                SyncLock mEvent
                    Monitor.PulseAll(mEvent)
                End SyncLock
            ElseIf status = O2GSessionStatusCode.Disconnected Then
                mSuccess = True
                SyncLock mEvent
                    Monitor.PulseAll(mEvent)
                End SyncLock
            End If
        End Sub
    End Class
End Namespace
