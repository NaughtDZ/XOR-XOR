Public Class Form1
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckForIllegalCrossThreadCalls = False '我知道其中的风险，只是这个程序不需要那么多花里胡哨的东西
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
        'Debug.WriteLine(listrightdulclick)
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
        ' Debug.WriteLine(listrightdulclick)
        If listrightdulclick >= 5 Then
            listrightdulclick = 0
            Timer1.Enabled = False
        End If
    End Sub
    ''' <summary>
    ''' 对文件进行处理和调用加密
    ''' </summary>
    ''' <param name="config"> 'filelocal As String, filesave As String, password As String</param>
    Private Sub Mainwork(config As Object())
        Try
            Dim fsave As New IO.FileStream(config(1), IO.FileMode.Create) '备写文件
            Dim opened_file As IO.FileStream = IO.File.OpenRead(config(0))  '读取的文件
            Dim run_thread(7) As Threading.Thread '单个文件拆8个线程
            Dim split_procce(7) As Xor_work '8个处理类
            Dim x1 As Long = 0, x2 As Long = 0, x3() As Byte, x4 As String = config(2), parameter(2) As Object 'bystar As Long, count As Long, x3 As Byte(), password As String
            If opened_file.Length > 8 Then '文件可以被拆分，事实上，真的有人会加密8个字节都没有的文件？？？
                If opened_file.Length Mod 8 = 0 Then '文件恰好可以被拆成8个字节相等的部分,该程序这么判定是因为不想自作主张往你的文件里写00，这样会导致解密后文件md5可能和原始文件对不上
                    '解决是否文件恰好可以被拆成8个字节相等的部分,该程序这么判定是因为不想自作主张往你的文件里写00，这样会导致解密后文件md5可能和原始文件对不上
                    x2 = (opened_file.Length - opened_file.Length Mod 8) / 8 '确定分段长度
                    ReDim x3(x2 - 1)
                    For fill = 0 To x2 - 1
                        x3(fill) = 0
                    Next
                    For n1 = 0 To 6
                        opened_file.Read(x3, x1, x2) '分段加密
                        parameter(0) = x2 : parameter(1) = x3 : parameter(2) = x4 '准备好线程中要传递参数
                        run_thread(n1) = New Threading.Thread(AddressOf split_procce(n1).xor_working) '创建当前分段线程
                        run_thread(n1).Start(parameter) '运行当前分段线程
                        x1 += (opened_file.Length - opened_file.Length Mod 8) / 8 '移动到下一段
                    Next
                    '单独处理最后一份
                    x2 = ((opened_file.Length - opened_file.Length Mod 8) / 8) + opened_file.Length Mod 8 '确定分段长度
                    ReDim x3(x2 - 1)
                    For fill = 0 To x2 - 1
                        x3(fill) = 0
                    Next
                    opened_file.Read(x3, x1, x2) '赋值给x3需要分段加密内容
                    parameter(0) = x2 : parameter(1) = x3 : parameter(2) = x4 '准备好线程中要传递参数
                    run_thread(7) = New Threading.Thread(AddressOf split_procce(7).xor_working) '创建当前分段线程
                    run_thread(7).Start(parameter) '运行当前分段线程
                End If
                'run_thread(0).Join() : run_thread(1).Join() : run_thread(2).Join() : run_thread(3).Join() : run_thread(4).Join() : run_thread(5).Join() : run_thread(6).Join() : run_thread(7).Join()
                For n2 = 1 To 7
                    split_procce(0).main_result.AddRange(split_procce(n2).main_result)
                Next
                fsave.Write(split_procce(0).main_result.ToArray(Type.GetType("System.Byte")), 0, opened_file.Length)
            Else '做个人吧，8个字节都不到的文件你还要加密？？？
                Dim onetime_ok As New Xor_work
                Dim onece() As Object = {opened_file.Length, IO.File.ReadAllBytes(config(0)), config(2)}
                onetime_ok.xor_working(onece)
                Dim be_write As Byte() = onetime_ok.main_result.ToArray(Type.GetType("System.Byte"))
                fsave.Write(be_write, 0, be_write.Length)
            End If
            '------------------------------------老的单线程处理
            'Dim fs As New IO.FileStream(config(0), IO.FileMode.Open) '读
            'Dim bynow As Byte, byenkey As Byte
            'Dim passwordbyt = System.Text.Encoding.Unicode.GetBytes(config(2)), passwordbyte_count As Integer, password_now_byte As Integer = 0 '密码大于一个字节的话，得换着字节去对应文件字节加密
            'passwordbyte_count = passwordbyt.Length - 1
            'For i = 0 To fs.Length - 1
            'bynow = fs.ReadByte()
            'byenkey = BitConverter.GetBytes(bynow Xor passwordbyt(password_now_byte))(0) 'XOR加密/解密操作，加转换防止溢出BUG
            'If password_now_byte < passwordbyte_count Then
            'password_now_byte += 1
            'Else
            'password_now_byte = 0
            'End If
            'fsave.WriteByte(byenkey)
            'Next
            '-----------------------------------
            fsave.Close()
            fsave.Dispose()
            ProgressBar1.Value += 1
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
                Dim config As Object() = New Object(2) {files, xorfile, TextBox2.Text}
                Threading.ThreadPool.QueueUserWorkItem(AddressOf Mainwork, config)
                ' Mainwork(config)
                ' ProgressBar1.Value += 1
            Next
        End If
    End Sub
End Class
''' <summary>
''' 曲线救国的多线程分段xor加密，方便获得处理结果
''' </summary>
Class Xor_work
    Friend main_result As ArrayList
    ''' <summary>
    ''' XOR加密过程
    ''' </summary>
    ''' <param name="paramer"> count As Long, x3 As Byte(), password As String</param>
    ''' <returns>返回的是加密后的字节组</returns>
    Function xor_working(paramer As Object()) As ArrayList
        Dim passwordbyt = System.Text.Encoding.Unicode.GetBytes(paramer(2)), passwordbyte_count As Integer, password_now_byte As Integer = 0 '密码大于一个字节的话，得换着字节去对应文件字节加密
        Dim byenkey As Byte, result As New ArrayList
        passwordbyte_count = passwordbyt.Length - 1
        For i = 0 To paramer(0) - 1 '拆分工作和计数工作应该在主任务里就完成了
            byenkey = BitConverter.GetBytes(paramer(1)(i) Xor passwordbyt(password_now_byte))(0) 'XOR加密/解密操作，加转换防止溢出BUG
            If password_now_byte < passwordbyte_count Then
                password_now_byte += 1
            Else
                password_now_byte = 0
            End If
            result.Add(byenkey)
        Next
        main_result = result
        Return result '本函数是为了上门的拆分的
    End Function
End Class

