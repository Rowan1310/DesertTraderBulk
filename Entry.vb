
Imports System
Imports System.Reflection
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports StardewModdingAPI
Imports StardewModdingAPI.Events
Imports StardewValley
Imports StardewValley.Menus

Public Class Entry
    Inherits [Mod]

    Private Const TROVE_ID As String = "(O)275"
    Private Const OMNI_ID As String = "(O)749"
    Private Const EXCHANGE_RATE As Integer = 5

    Private btn10 As Rectangle
    Private btnMax As Rectangle

    Private showButtons As Boolean = False
    Private buttonsReady As Boolean = False

    Public Overrides Sub Entry(helper As IModHelper)

        AddHandler helper.Events.Display.MenuChanged,
            AddressOf OnMenuChanged

        AddHandler helper.Events.Display.RenderedActiveMenu,
            AddressOf OnRenderedActiveMenu

        AddHandler helper.Events.Input.ButtonPressed,
            AddressOf OnButtonPressed

        Monitor.Log(
            "Desert Trader Bulk loaded!",
            LogLevel.Info
        )

    End Sub

    Private Sub OnMenuChanged(
        sender As Object,
        e As MenuChangedEventArgs
    )

        Dim shop As ShopMenu =
            TryCast(e.NewMenu, ShopMenu)

        showButtons = shop IsNot Nothing
        buttonsReady = False

    End Sub

    ' ==================================================
    ' DETEKSI ARTIFACT TROVE DI DAFTAR TOKO
    ' ==================================================

    Private Function HasArtifactTrove(
        shop As ShopMenu
    ) As Boolean

        If shop Is Nothing Then Return False

        Try

            For Each item As StardewValley.Item In shop.forSale

                If item IsNot Nothing AndAlso
                   item.QualifiedItemId = TROVE_ID Then

                    Return True

                End If

            Next

        Catch ex As Exception

            Monitor.Log(
                "Shop item detection failed: " & ex.Message,
                LogLevel.Warn
            )

        End Try

        Return False

    End Function

    ' ==================================================
    ' MENGGAMBAR TOMBOL
    ' ==================================================

    Private Sub OnRenderedActiveMenu(
        sender As Object,
        e As RenderedActiveMenuEventArgs
    )

        buttonsReady = False

        If Not showButtons Then Return
        If Not Context.IsWorldReady Then Return

        Dim shop As ShopMenu =
            TryCast(Game1.activeClickableMenu, ShopMenu)

        If shop Is Nothing Then Return

        ' Tombol hanya muncul jika Artifact Trove
        ' tersedia dalam daftar barang toko.

        If Not HasArtifactTrove(shop) Then Return

        Dim centerX As Integer =
            Game1.uiViewport.Width \ 2

        Dim y As Integer = 45

        btn10 = New Rectangle(
            centerX - 205,
            y,
            190,
            64
        )

        btnMax = New Rectangle(
            centerX + 15,
            y,
            190,
            64
        )

        DrawButton(
            e.SpriteBatch,
            btn10,
            "Tukar x10"
        )

        DrawButton(
            e.SpriteBatch,
            btnMax,
            "Tukar MAX"
        )

        buttonsReady = True

    End Sub

    Private Sub DrawButton(
        sb As SpriteBatch,
        r As Rectangle,
        buttonText As String
    )

        sb.Draw(
            Game1.staminaRect,
            r,
            Color.Black * 0.85F
        )

        Dim font = Game1.dialogueFont

        Dim size As Vector2 =
            font.MeasureString(buttonText)

        Dim position As New Vector2(
            r.X + (r.Width - size.X) / 2,
            r.Y + (r.Height - size.Y) / 2
        )

        sb.DrawString(
            font,
            buttonText,
            position,
            Color.Yellow
        )

    End Sub

    ' ==================================================
    ' MENANGANI KLIK TOMBOL
    ' ==================================================

    Private Sub OnButtonPressed(
        sender As Object,
        e As ButtonPressedEventArgs
    )

        If Not Context.IsWorldReady Then Return
        If Not showButtons Then Return
        If Not buttonsReady Then Return

        If e.Button <> SButton.MouseLeft Then Return

        Dim shop As ShopMenu =
            TryCast(Game1.activeClickableMenu, ShopMenu)

        If shop Is Nothing Then Return
        If Not HasArtifactTrove(shop) Then Return

        ' Gunakan koordinat UI game.
        Dim mousePosition As New Point(
            Game1.getMouseX(),
            Game1.getMouseY()
        )

        Dim amount As Integer = 0

        If btn10.Contains(mousePosition) Then

            amount = 10

        ElseIf btnMax.Contains(mousePosition) Then

            Dim available As Integer =
                CountOmni()

            amount = available \ EXCHANGE_RATE

        Else

            ' Klik barang toko biasa tidak melakukan pertukaran.
            Return

        End If

        ' Cegah klik tombol diteruskan ke menu toko.
        Helper.Input.Suppress(e.Button)

        If amount <= 0 Then

            Game1.playSound("cancel")
            Return

        End If

        ExchangeOmni(amount)

    End Sub

    ' ==================================================
    ' PROSES PERTUKARAN
    ' ==================================================

    Private Sub ExchangeOmni(requestedAmount As Integer)

        Dim availableOmni As Integer =
            CountOmni()

        Dim maximumAmount As Integer =
            availableOmni \ EXCHANGE_RATE

        Dim amount As Integer =
            Math.Min(requestedAmount, maximumAmount)

        If amount <= 0 Then Return

        Dim added As Integer = 0

        Try

            For i As Integer = 1 To amount

                ' Artifact Trove menggunakan ID object 275.
                Dim trove As StardewValley.Item =
                    ItemRegistry.Create(TROVE_ID)

                If trove Is Nothing Then

                    Monitor.Log(
                        "Artifact Trove could not be created.",
                        LogLevel.Error
                    )

                    Exit For

                End If

                ' Masukkan trove ke inventaris.
                If Not Game1.player.addItemToInventoryBool(trove) Then

                    Monitor.Log(
                        "Inventory is full. Exchange stopped.",
                        LogLevel.Warn
                    )

                    Exit For

                End If

                added += 1

            Next

            ' Potong Omni Geode hanya sebanyak
            ' Artifact Trove yang berhasil masuk inventaris.

            If added > 0 Then

                RemoveOmni(added * EXCHANGE_RATE)

                Game1.playSound("coin")

                Monitor.Log(
                    "Successfully exchanged " &
                    added.ToString() &
                    " Artifact Trove.",
                    LogLevel.Info
                )

            Else

                Game1.playSound("cancel")

            End If

        Catch ex As Exception

            Monitor.Log(
                "Exchange failed: " & ex.ToString(),
                LogLevel.Error
            )

        End Try

    End Sub

    ' ==================================================
    ' MEMBACA JUMLAH STACK
    ' ==================================================

    Private Function GetItemStack(
        item As StardewValley.Item
    ) As Integer

        If item Is Nothing Then Return 0

        Dim itemType As Type = item.GetType()

        Dim stackProperty As PropertyInfo =
            itemType.GetProperty(
                "Stack",
                BindingFlags.Public Or
                BindingFlags.NonPublic Or
                BindingFlags.Instance
            )

        If stackProperty IsNot Nothing Then

            Return CInt(stackProperty.GetValue(item))

        End If

        ' Cari field pada tipe dan kelas induknya.
        Dim currentType As Type = itemType

        While currentType IsNot Nothing

            Dim stackField As FieldInfo =
                currentType.GetField(
                    "stack",
                    BindingFlags.Public Or
                    BindingFlags.NonPublic Or
                    BindingFlags.Instance Or
                    BindingFlags.DeclaredOnly
                )

            If stackField IsNot Nothing Then

                Return CInt(stackField.GetValue(item))

            End If

            currentType = currentType.BaseType

        End While

        Throw New MissingMemberException(
            "Unable to find item stack member."
        )

    End Function

    ' ==================================================
    ' MENGUBAH JUMLAH STACK
    ' ==================================================

    Private Sub SetItemStack(
        item As StardewValley.Item,
        value As Integer
    )

        If item Is Nothing Then Return

        Dim itemType As Type = item.GetType()

        Dim stackProperty As PropertyInfo =
            itemType.GetProperty(
                "Stack",
                BindingFlags.Public Or
                BindingFlags.NonPublic Or
                BindingFlags.Instance
            )

        If stackProperty IsNot Nothing AndAlso
           stackProperty.CanWrite Then

            stackProperty.SetValue(item, value)
            Return

        End If

        Dim currentType As Type = itemType

        While currentType IsNot Nothing

            Dim stackField As FieldInfo =
                currentType.GetField(
                    "stack",
                    BindingFlags.Public Or
                    BindingFlags.NonPublic Or
                    BindingFlags.Instance Or
                    BindingFlags.DeclaredOnly
                )

            If stackField IsNot Nothing Then

                stackField.SetValue(item, value)
                Return

            End If

            currentType = currentType.BaseType

        End While

        Throw New MissingMemberException(
            "Unable to set item stack member."
        )

    End Sub

    ' ==================================================
    ' MENGHITUNG OMNI GEODE
    ' ==================================================

    Private Function CountOmni() As Integer

        Dim total As Integer = 0

        For Each item As StardewValley.Item In Game1.player.Items

            If item IsNot Nothing AndAlso
               item.QualifiedItemId = OMNI_ID Then

                total += GetItemStack(item)

            End If

        Next

        Return total

    End Function

    ' ==================================================
    ' MENGURANGI OMNI GEODE
    ' ==================================================

    Private Sub RemoveOmni(need As Integer)

        If need <= 0 Then Return

        For i As Integer = 0 To Game1.player.Items.Count - 1

            If need <= 0 Then Exit For

            Dim item As StardewValley.Item =
                Game1.player.Items(i)

            If item Is Nothing Then Continue For

            If item.QualifiedItemId = OMNI_ID Then

                Dim currentStack As Integer =
                    GetItemStack(item)

                Dim take As Integer =
                    Math.Min(currentStack, need)

                Dim remaining As Integer =
                    currentStack - take

                SetItemStack(item, remaining)

                need -= take

                If remaining <= 0 Then

                    Game1.player.Items(i) = Nothing

                End If

            End If

        Next

    End Sub

End Class
                                                    
