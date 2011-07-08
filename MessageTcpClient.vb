Imports System.Xml.Serialization
Imports System.Net.Sockets
Imports System.Text
Imports System.IO

' Ακολουθεί ένα σύστημα δικτυακής επικοινωνίας μέσω TCP sockets. Το σύστημα
' στέλνει και λαμβάνει μηνύματα με κωδικοποίηση XML. Τα μηνύματα που στέλνει
' και λαμβάνει έχουν συγκεκριμένους τύπους.
Public Class MessageTcpClient(Of IncomingType, OutgoingType)
    Private Const BUFFER_SIZE As Integer = 1024

    ' Ο TcpClient που αναλαμβάνει την επικοινωνία
    Private tcpClient As TcpClient

    ' Προσωρινός χώρος αποθήκευσης για τα εισερχόμενα δεδομένα
    Private recvBuffer() As Byte

    ' Προσωρινός χώρος αποθήκευσης για την εισερχόμενη επικοινωνία
    Private buffer As List(Of Byte)

    ' Δηλώνει πως ο client είναι συνδεδεμένος
    Private connected As Boolean

    ' Το παρακάτω γεγονός ειδοποιεί για τη λήψη ενός μηνύματος από το δίκτυο.
    Public Event MessageReceived(ByVal client As MessageTcpClient(Of IncomingType, OutgoingType), ByVal message As IncomingType)

    ' Το παρακάτω γεγονός ειδοποιεί για τον τερματισμό της επικοινωνίας με το
    ' δίκτυο.
    Public Event Close(ByVal client As MessageTcpClient(Of IncomingType, OutgoingType))

    ' Αρχικοποιεί το σύστημα.
    Public Sub New(ByVal tcpClient As TcpClient)
        ' Αρχικοποίηση πεδίων
        Me.tcpClient = tcpClient
        Me.recvBuffer = New Byte(BUFFER_SIZE) {}
        Me.buffer = New List(Of Byte)
        Me.connected = tcpClient.Connected

        ' Εκκίνηση της διαδικασίας ανάγνωσης από το δίκτυο.
        Me.tcpClient.GetStream().BeginRead(Me.recvBuffer, 0, Me.recvBuffer.Length, AddressOf ReadCallback, Nothing)
    End Sub

    ' Η παρακάτω υπορουτίνα καλείται όταν έρθουν δεδομένα από το δίκτυο.
    Private Sub ReadCallback(ByVal ar As IAsyncResult)
        Dim bytesRead As Integer

        ' Δοκιμάζει αν υπάρχουν δεδομένα για ανάγνωση. Αν δεν υπάρχουν
        ' σημαίνει ότι υπάρχει πρόβλημα επικοινωνίας.
        Try
            bytesRead = tcpClient.GetStream().EndRead(ar)
        Catch

            ' Ειδοποιεί πως η επικοινωνία έχει διακοπεί.
            Me.Disconnect()
            Return
        End Try

        If bytesRead = 0 Then
            Me.Disconnect()
            Return
        End If

        Dim tmp(bytesRead - 1) As Byte
        Array.Copy(recvBuffer, tmp, bytesRead)
        buffer.AddRange(tmp)
        Dim separatorPos As Integer
        While True
            separatorPos = buffer.IndexOf(CType(0, Byte))
            If separatorPos < 0 Then
                Exit While
            End If
            Dim ok = Me.ProcessMessage(Encoding.UTF8.GetString(buffer.GetRange(0, separatorPos).ToArray))
            If Not ok Then
                Exit Sub
            End If
            buffer.RemoveRange(0, separatorPos + 1)
        End While

        ' Συνεχίζει την ανάγνωση από το δίκτυο. Αν υπάρξει πρόβλημα, ειδοποιεί
        ' με κατάλληλο event.
        Try
            tcpClient.GetStream().BeginRead(recvBuffer, 0, recvBuffer.Length, AddressOf ReadCallback, Nothing)
        Catch
            Me.Disconnect()
            Return
        End Try
    End Sub

    ' Η παρακάτω υπορουτίνα καλείται όταν έχει ληφθεί ένα ολοκληρωμένο μήνυμα
    ' για επεξεργασία.
    Private Function ProcessMessage(ByVal XMLMessage As String) As Boolean
        ' Μετατρέπει το μήνυμα από XML.
        Dim xmlSerializer As New XmlSerializer(GetType(IncomingType), "")
        Dim stringReader As New StringReader(XMLMessage)
        Try
            Dim message As IncomingType
            message = CType(xmlSerializer.Deserialize(stringReader), IncomingType)

            ' Στέλνει event που ενημερώνει για τη λήψη του μηνύματος.
            RaiseEvent MessageReceived(Me, message)
            Return True
        Catch
            ' Αν ληφθούν λάθος δεδομένα τότε διακόπτεται η επικοινωνία.
            Me.Disconnect()
            Return False
        End Try
    End Function

    ' Η παρακάτω υπορούτινα χρησιμοποιείται για την αποστολή μηνυμάτων.
    Public Sub SendMessage(ByVal message As OutgoingType)
        If Not Me.connected Then
            Exit Sub
        End If

        ' Το μήνυμα μετατρέπεται σε XML και αποστέλλεται.
        Dim xmlSerializer As New XmlSerializer(message.GetType(), "")
        xmlSerializer.Serialize(Me.tcpClient.GetStream(), message)

        ' Αποστέλλεται ένα διαχωριστικό byte με τιμή 0.
        Dim zero As Byte() = {0}
        Me.tcpClient.GetStream().Write(zero, 0, zero.Length)
    End Sub

    ' Επιστρέφει τα στοιχεία του απομακρυσμένου άκρου επικοινωνίας.
    Public Function GetRemoteEndPoint() As Net.EndPoint
        Return Me.tcpClient.Client.RemoteEndPoint
    End Function

    ' Τερματίζει την επικοινωνία.
    Public Sub Disconnect()
        If Me.connected Then
            Me.connected = False
            RaiseEvent Close(Me)
            Me.tcpClient.Close()
        End If
    End Sub
End Class
