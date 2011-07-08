Imports System.Net.Sockets
Imports System.Net

' Το παρακάτω σύστημα υλοποιεί έναν απλό server που δέχεται συνδέσεις και
' μπορεί να στείλει και να λάβει μηνύματα από/σε αυτές.
Public Class MessageServer(Of IncomingType, OutgoingType)
    Private Const SERVER_PORT As Integer = 8000

    ' Ο TcpListener που υλοποιεί το server
    Private tcpListener As TcpListener

    ' Ξεκινάει το server.
    Public Sub StartServer()
        tcpListener = New TcpListener(IPAddress.Any, SERVER_PORT)
        tcpListener.Start()
        tcpListener.BeginAcceptTcpClient(AddressOf AcceptCallback, Nothing)
    End Sub

    ' Η παρακάτω ρουτίνα καλείται όταν γίνει αίτηση για σύνδεση από κάποιον
    ' client.
    Private Sub AcceptCallback(ByVal ar As IAsyncResult)
        Try
            Dim client As MessageTcpClient(Of IncomingType, OutgoingType)
            client = New MessageTcpClient(Of IncomingType, OutgoingType)(tcpListener.EndAcceptTcpClient(ar))
            Me.OnNewClient(client)

            Me.tcpListener.BeginAcceptTcpClient(AddressOf AcceptCallback, Nothing)
        Catch e As ObjectDisposedException
            ' Ο server δε δέχεται πλέον νέες συνδέσεις.
        End Try
    End Sub

    ' Σταματάει το server.
    Public Sub StopServer()
        tcpListener.Stop()
    End Sub

    ' Η παρακάτω ρουτίνα καλείται όταν συνδεθεί ένας νέος client.
    Protected Overridable Sub OnNewClient(ByVal client As MessageTcpClient(Of IncomingType, OutgoingType))

    End Sub
End Class
