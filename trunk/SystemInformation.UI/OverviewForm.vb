Imports System.Net
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.IO
Imports Microsoft.Win32
Imports System.ComponentModel

Public Class OverviewForm

    Dim drag As Boolean
    Dim mousex As Integer
    Dim mousey As Integer

#Region "Basic Information"

    ''' <summary>
    ''' Private variable for storing CPU usage information
    ''' </summary>
    ''' <remarks></remarks>
    Private m_PerformanceCounter As New System.Diagnostics.PerformanceCounter("Processor", "% Processor Time", "_Total")

    ''' <summary>
    ''' Retrieves system name
    ''' </summary>
    ''' <returns>System Name</returns>
    ''' <remarks></remarks>
    Private Function GetSystemName() As String
        Return My.Computer.Name
    End Function

    ''' <summary>
    ''' Retrieves operating system
    ''' </summary>
    ''' <returns>OS Name</returns>
    ''' <remarks></remarks>
    Private Function GetOperatingSystem() As String
        Return String.Format("{0} {1}", My.Computer.Info.OSFullName, GetBitsVersion)
    End Function

    ''' <summary>
    ''' Retrieves OS version
    ''' </summary>
    ''' <returns>OS Version</returns>
    ''' <remarks></remarks>
    Private Function GetBitsVersion() As String
        If Environment.Is64BitOperatingSystem Then
            Return "x64"
        Else
            Return "x86"
        End If
    End Function

    ''' <summary>
    ''' Retrieves total available memory
    ''' </summary>
    ''' <returns>Total Memory</returns>
    ''' <remarks></remarks>
    Private Function GetTotalMemory() As String
        Return String.Format("{0}", System.Math.Round(My.Computer.Info.TotalPhysicalMemory / (1024 * 1024)), 2)
    End Function

    ''' <summary>
    ''' Retrieves free memory
    ''' </summary>
    ''' <returns>Free Memory</returns>
    ''' <remarks></remarks>
    Private Function GetAvailableMemory() As String
        Return String.Format("{0}", System.Math.Round(My.Computer.Info.AvailablePhysicalMemory / (1024 * 1024)), 2)
    End Function

    ''' <summary>
    ''' Retrieves CPU usage
    ''' </summary>
    ''' <returns>CPU Usage</returns>
    ''' <remarks></remarks>
    Private Function GetCPUUsage() As String
        Return String.Format("{0}%", CInt(m_PerformanceCounter.NextValue()))
    End Function

#End Region

#Region "Network Information"

    ''' <summary>
    ''' Retrieves host IP address
    ''' </summary>
    ''' <returns>IP Address</returns>
    ''' <remarks></remarks>
    Private Function GetInternalIP() As String
        Return Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(Function(a As IPAddress) Not a.IsIPv6LinkLocal AndAlso Not a.IsIPv6Multicast AndAlso Not a.IsIPv6SiteLocal).First.ToString
    End Function

    ''' <summary>
    ''' Retrieves external IP address
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetExternalIP() As String
        Dim ip As String
        ip = (New WebClient()).DownloadString("http://checkip.dyndns.org/")
        ip = (New Regex("\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Matches(ip)(0).ToString
        Return ip

    End Function
#End Region

#Region "Drive info"
    ''' <summary>
    ''' Grabs all attached drive information.
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub GetDriveInfo()
        For Each drive As DriveInfo In My.Computer.FileSystem.Drives
            Try
                Dim itm As New ListViewItem
                With itm
                    .Text = drive.Name
                    .SubItems.Add(GetDriveType(drive.DriveType))
                    .SubItems.Add(drive.DriveFormat)
                    .SubItems.Add(String.Format("{0} GB", FormatNumber(drive.TotalSize / ((1024 * 1024) * 1024), 2)))
                    .SubItems.Add(String.Format("{0} GB", FormatNumber(drive.AvailableFreeSpace / ((1024 * 1024) * 1024), 2)))
                End With

                AddToListview(DrivesListView, itm)
            Catch ex As IOException
                ' Skip this is there is an IO Exception
            End Try

        Next
    End Sub
    ''' <summary>
    ''' Works out drive type
    ''' </summary>
    ''' <param name="_id"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function GetDriveType(_id As Integer) As String
        Select Case _id
            Case 1
                Return DriveType.NoRootDirectory.ToString
            Case 2
                Return DriveType.Removable.ToString
            Case 3
                Return DriveType.Fixed.ToString
            Case 4
                Return DriveType.Network.ToString
            Case 5
                Return DriveType.CDRom.ToString
            Case 6
                Return DriveType.Ram.ToString
            Case Else
                Return DriveType.Unknown.ToString
        End Select

    End Function

#End Region

#Region "User Information"

    ''' <summary>
    ''' Grabs the list of users based on the profiles found in the registry
    ''' </summary>
    ''' <remarks></remarks>
    Private Sub GetLocalUsers()
        Dim _regKey As RegistryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList")

        For Each _keyName As String In _regKey.GetSubKeyNames
            Using subKey As RegistryKey = _regKey.OpenSubKey(_keyName)
                Dim _users As String = DirectCast(subKey.GetValue("ProfileImagePath"), String)
                Dim _Username As String = System.IO.Path.GetFileNameWithoutExtension(_users)
                Dim itm As New ListViewItem
                itm.Text = _Username
                AddToListview(WindowsUserListView, itm)
            End Using
        Next
    End Sub

#End Region

    Private Sub OverviewForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If IsOnline() Then
            Run(True)
        Else
            RefreshTimer.Enabled = False
            MsgBox("This application needs an internet connection to obtain your external internet protocal address.", vbOKOnly + MsgBoxStyle.Information, "Unable to connect")
            End
        End If

    End Sub

    Private Sub Run(Optional _AccRefresh As Boolean = False)
        ' Basic Information
        Me.FormTitleLabel.Text = Me.Text
        AddToString(Me.ComputerNameLabelText, String.Format("{0}", GetSystemName))
        AddToString(Me.OperatingSystemLabelText, String.Format("{0}", GetOperatingSystem))
        AddToString(Me.AvailableMemoryLabelText, String.Format("{0} MB / {1} MB", FormatNumber(GetAvailableMemory), FormatNumber(GetTotalMemory)))
        AddToString(Me.CPUUsageLabelText, String.Format("{0}", GetCPUUsage))

        '' Network Information
        Me.InternalIPLabelText.Text = GetInternalIP()
        Me.ExternalIPLabelText.Text = GetExternalIP().ToString

        ClearListview(DrivesListView)
        '' Drive Information
        Call GetDriveInfo()

        If _AccRefresh Then
            ClearListview(WindowsUserListView)
            '' Local Users
            Call GetLocalUsers()
        End If
    End Sub

    Private Sub RefreshTimer_Tick(sender As Object, e As EventArgs) Handles RefreshTimer.Tick
        If Not bw.IsBusy Then
            bw.RunWorkerAsync()
        End If
    End Sub
    ''' <summary>
    ''' Attempts to ping Google to see whether PC has internet connection.
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Private Function IsOnline() As Boolean
        Dim url As New System.Uri("http://www.google.co.uk")
        Dim req As System.Net.WebRequest
        req = System.Net.WebRequest.Create(url)
        Dim resp As System.Net.WebResponse
        Try
            resp = req.GetResponse
            resp.Close()
            req = Nothing
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

#Region "Exit"

    Private Sub CloseButton_Click(sender As Object, e As EventArgs) Handles CloseButton.Click
        End
    End Sub

    Private Sub ExitButton_Click(sender As Object, e As EventArgs) Handles ExitButton.Click
        End
    End Sub

#End Region

#Region "Movement"

    Private Sub OverviewForm_MouseDown(sender As Object, e As MouseEventArgs) Handles Me.MouseDown
        drag = True
        mousex = Windows.Forms.Cursor.Position.X - Me.Left
        mousey = Windows.Forms.Cursor.Position.Y - Me.Top

    End Sub

    Private Sub OverviewForm_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        If drag Then
            Me.Top = Windows.Forms.Cursor.Position.Y - mousey
            Me.Left = Windows.Forms.Cursor.Position.X - mousex
        End If
    End Sub

    Private Sub OverviewForm_MouseUp(sender As Object, e As MouseEventArgs) Handles Me.MouseUp
        drag = False
    End Sub

    Private Sub FormTitleLabel_MouseDown(sender As Object, e As MouseEventArgs) Handles FormTitleLabel.MouseDown
        drag = True
        mousex = Windows.Forms.Cursor.Position.X - Me.Left
        mousey = Windows.Forms.Cursor.Position.Y - Me.Top
    End Sub

    Private Sub FormTitleLabel_MouseMove(sender As Object, e As MouseEventArgs) Handles FormTitleLabel.MouseMove
        If drag Then
            Me.Top = Windows.Forms.Cursor.Position.Y - mousey
            Me.Left = Windows.Forms.Cursor.Position.X - mousex
        End If
    End Sub

    Private Sub FormTitleLabel_MouseUp(sender As Object, e As MouseEventArgs) Handles FormTitleLabel.MouseUp
        drag = False
    End Sub

#End Region

    Private bw As BackgroundWorker = New BackgroundWorker

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()
        bw.WorkerReportsProgress = True
        ' Add any initialization after the InitializeComponent() call.

        AddHandler bw.DoWork, AddressOf bw_DoWork
        AddHandler bw.RunWorkerCompleted, AddressOf bw_RunWorkerCompleted
        AddHandler bw.ProgressChanged, AddressOf bw_ProgressChanged
    End Sub

    Private Sub bw_DoWork(sender As Object, e As DoWorkEventArgs)
        Run()
        'Me.CPUUsageLabelText.Refresh()
        'Me.AvailableMemoryLabelText.Refresh()
    End Sub

    Private Sub bw_RunWorkerCompleted(sender As Object, e As RunWorkerCompletedEventArgs)

    End Sub

    Private Sub bw_ProgressChanged(sender As Object, e As ProgressChangedEventArgs)

    End Sub

    Delegate Sub AddToString_Delegate([Label] As Label, [Text] As String)
    Public Sub AddToString([Label] As Label, [Text] As String)
        If [Label].InvokeRequired Then
            Dim MyDel As New AddToString_Delegate(AddressOf AddToString)
            Me.Invoke(MyDel, New Object() {[Label], [Text]})
        Else
            [Label].Text = [Text]
        End If
    End Sub

    Delegate Sub AddToListview_Delegate([Label] As ListView, [Text] As ListViewItem)
    Public Sub AddToListview([Label] As ListView, [Text] As ListViewItem)
        If [Label].InvokeRequired Then
            Dim MyDel As New AddToListview_Delegate(AddressOf AddToListview)
            Me.Invoke(MyDel, New Object() {[Label], [Text]})
        Else
            Label.Items.Add([Text])
        End If
    End Sub

    Delegate Sub ClearListview_Delegate([Label] As ListView)
    Public Sub ClearListview([Label] As ListView)
        If [Label].InvokeRequired Then
            Dim MyDel As New ClearListview_Delegate(AddressOf ClearListview)
            Me.Invoke(MyDel, New Object() {[Label]})
        Else
            Label.Items.Clear()
        End If
    End Sub

End Class