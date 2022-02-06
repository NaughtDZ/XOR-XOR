Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

    End Sub

    Private Sub Form1_Resize(sender As Object, e As EventArgs) Handles Me.Resize
        'Label1.Size = Me.ClientSize - New Size(Label1.Location)
        'Button3.Size = Me.ClientSize - New Size(Button3.Location)
        'ListBox1.Size = Me.ClientSize - New Size(ListBox1.Location)
        'ProgressBar1.Size = Me.ClientSize - New Size(ProgressBar1.Location)
        'ProgressBar2.Size = Me.ClientSize - New Size(ProgressBar2.Location)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        MsgBox("请牢记随机出来的密码！", MsgBoxStyle.Exclamation, "注意！")
        Randomize(System.DateTime.Now.Ticks)
        TextBox2.Text = Math.Round(Rnd() * 89999999999 + 1000000000)
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox1_DragDrop(sender As Object, e As DragEventArgs) Handles ListBox1.DragDrop
        If e.Data.GetDataPresent(DataFormats.FileDrop, False) Then
            Dim dragedfiles As String() = CType(e.Data.GetData(DataFormats.FileDrop), String())
            For Each s As String In dragedfiles
                ListBox1.Items.Add(s)
            Next
        End If
    End Sub

    Private Sub ListBox1_DragEnter(sender As Object, e As DragEventArgs) Handles ListBox1.DragEnter
        e.Effect = DragDropEffects.All
    End Sub

    Private Sub ListBox1_DoubleClick(sender As Object, e As EventArgs) Handles ListBox1.DoubleClick
        Try
            ListBox1.Items.RemoveAt(ListBox1.SelectedIndex)
        Catch
        End Try
    End Sub

    Private Sub ListBox1_MouseUp(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseUp
        Timer1.Enabled = True '兜一圈子就是为了双击清除列表
        Debug.WriteLine(listrightdulclick)
        If e.Button = MouseButtons.Right And listrightdulclick > 0 Then
            ListBox1.Items.Clear()
        ElseIf e.Button <> MouseButtons.Right Then
            Timer1.Enabled = False
            listrightdulclick = 0
        End If
    End Sub
    Dim listrightdulclick As Byte = 0

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        listrightdulclick += 1 'vb.net没有右键双击事件？？？
        Debug.WriteLine(listrightdulclick)
        If listrightdulclick >= 4 Then
            listrightdulclick = 0
            Timer1.Enabled = False
        End If
    End Sub

    Private Sub Mainwork(filelocal As String, filesave As String, password As String)
        Try
            Dim fs As New IO.FileStream(filelocal, IO.FileMode.Open) '读
            Dim fsave As New IO.FileStream(filesave, IO.FileMode.Create) '写
            Dim bynow As Byte, byenkey As Byte
            Dim passwordbyt = System.Text.Encoding.Unicode.GetBytes(password), passwordbyte_count As Integer, password_now_byte As Integer = 0 '密码大于一个字节的话，得换着字节去对应文件字节加密
            passwordbyte_count = passwordbyt.Length - 1
            ProgressBar2.Value = 0
            ProgressBar2.Maximum = fs.Length '进度条
            For i = 0 To fs.Length - 1
                bynow = fs.ReadByte()
                byenkey = BitConverter.GetBytes(bynow Xor passwordbyt(password_now_byte))(0) 'XOR加密/解密操作，加转换防止溢出BUG
                If password_now_byte < passwordbyte_count Then
                    password_now_byte += 1
                Else
                    passwordbyte_count = 0
                End If
                fsave.WriteByte(byenkey)
                ProgressBar2.Value += 1
            Next
            fsave.Close()
            fsave.Dispose()
            fs.Close()
            fs.Dispose()
        Catch e As Exception
            Debug.WriteLine(e.ToString)
            Exit Sub
        End Try
    End Sub



    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If TextBox2.Text = "" Then
            MsgBox("请输入密码", MsgBoxStyle.Critical, "没得密码？？！")
            Exit Sub
        End If
        Dim xorfile As String
        If ListBox1.Items.Count > 0 Then
            ProgressBar1.Value = 0
            ProgressBar1.Maximum = ListBox1.Items.Count
            For Each files In ListBox1.Items
                If InStr(IO.Path.GetFileName(files), "(_Xor_)", CompareMethod.Text) = 0 Then '添加或删去加密标记
                    xorfile = IO.Path.GetDirectoryName(files) & "\(_Xor_)" & IO.Path.GetFileName(files)
                Else
                    xorfile = Replace(files.ToString, "(_Xor_)", "",,, CompareMethod.Text)
                End If
                Mainwork(files, xorfile, TextBox2.Text)
                ProgressBar1.Value += 1
            Next
        End If
    End Sub
End Class



