
Imports System
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports StardewModdingAPI
Imports StardewModdingAPI.Events
Imports StardewValley
Imports StardewValley.Menus

Public Class Entry
    Inherits [Mod]

    Private Const TROVE_ID As String = "(O)275"

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

    ' ==================================================
    ' DETEKSI MENU TOKO
    ' ==================================================

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
    ' DETEKSI ARTIFACT TROVE
    ' ==================================================

    Private Function FindArtifactTroveIndex(
        shop As ShopMenu
    ) As Integer

        If shop Is Nothing Then Return -1

        For i As Integer = 0 To shop.forSale.Count - 1

            Dim item As StardewValley.Item =
                TryCast(shop.forSale(i), StardewValley.Item)

            If item IsNot Nothing AndAlso
               item.QualifiedItemId = TROVE_ID Then

                Return i

            End If

        Next

        Return -1

    End Function

    Private Function HasArtifactTrove(
        shop As ShopMenu
    ) As Boolean

        Return FindArtifactTroveIndex(shop) >= 0

    End Function

    ' ==================================================
    ' MENCARI POSISI TOMBOL PEMBELIAN NATIVE
    ' ==================================================

    Private Function GetTroveClickPoint(
        shop As ShopMenu,
        ByRef clickPoint As Point
    ) As Boolean

        Dim index As Integer =
            FindArtifactTroveIndex(shop)

        If index < 0 Then Return False

        Dim buttonIndex As Integer =
            index - shop.currentItemIndex

        If buttonIndex < 0 OrElse
           buttonIndex >= shop.forSaleButtons.Count Then

            Return False

        End If

        clickPoint =
            shop.forSaleButtons(buttonIndex).bounds.Center

        Return True

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
    ' JUMLAH MAKSIMUM STACK ARTIFACT TROVE
    ' ==================================================

    Private Function GetMaximumBatch(
        shop As ShopMenu
    ) As Integer

        If shop Is Nothing Then Return 0

        Dim held As StardewValley.Item =
            TryCast(shop.heldItem, StardewValley.Item)

        If held IsNot Nothing Then

            If held.QualifiedItemId <> TROVE_ID Then
                Return 0
            End If

            Return Math.Max(
                0,
                held.maximumStackSize() - held.Stack
            )

        End If

        Dim sample As StardewValley.Item =
            ItemRegistry.Create(TROVE_ID)

        If sample Is Nothing Then Return 0

        Return Math.Max(
            1,
            sample.maximumStackSize()
        )

    End Function

    ' ==================================================
    ' MEMBACA STACK TROVE YANG SEDANG DIPEGANG
    ' ==================================================

    Private Function GetHeldTroveStack(
        shop As ShopMenu
    ) As Integer

        If shop Is Nothing Then Return 0

        Dim held As StardewValley.Item =
            TryCast(shop.heldItem, StardewValley.Item)

        If held Is Nothing Then Return 0

        If held.QualifiedItemId <> TROVE_ID Then
            Return 0
        End If

        Return held.Stack

    End Function

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

        Dim mousePosition As New Point(
            Game1.getMouseX(),
            Game1.getMouseY()
        )

        Dim amount As Integer = 0

        If btn10.Contains(mousePosition) Then

            amount = 10

        ElseIf btnMax.Contains(mousePosition) Then

            amount = GetMaximumBatch(shop)

        Else

            Return

        End If

        Helper.Input.Suppress(e.Button)

        If amount <= 0 Then

            Game1.playSound("cancel")
            Return

        End If

        ExchangeThroughNativeShop(
            shop,
            amount
        )

    End Sub

    ' ==================================================
    ' TRANSAKSI MELALUI SHOPMENU NATIVE
    ' ==================================================

    Private Sub ExchangeThroughNativeShop(
        shop As ShopMenu,
        requestedAmount As Integer
    )

        If shop Is Nothing Then Return

        If Game1.activeClickableMenu IsNot shop Then
            Return
        End If

        Dim existingHeld As StardewValley.Item =
            TryCast(shop.heldItem, StardewValley.Item)

        If existingHeld IsNot Nothing AndAlso
           existingHeld.QualifiedItemId <> TROVE_ID Then

            Game1.playSound("cancel")
            Return

        End If

        Dim successfulPurchases As Integer = 0

        Try

            For i As Integer = 1 To requestedAmount

                If Game1.activeClickableMenu IsNot shop Then
                    Exit For
                End If

                Dim clickPoint As Point

                If Not GetTroveClickPoint(
                    shop,
                    clickPoint
                ) Then

                    Monitor.Log(
                        "Artifact Trove shop button is not visible.",
                        LogLevel.Warn
                    )

                    Exit For

                End If

                Dim previousStack As Integer =
                    GetHeldTroveStack(shop)

                ' PANGGIL TRANSAKSI SHOPMENU NATIVE.
                ' Harmony patch Building Chest Supply
                ' berkesempatan memproses material di sini.

                shop.receiveLeftClick(
                    clickPoint.X,
                    clickPoint.Y,
                    True
                )

                Dim currentStack As Integer =
                    GetHeldTroveStack(shop)

                ' Tidak ada penambahan stack berarti
                ' transaksi gagal atau material habis.

                If currentStack <= previousStack Then
                    Exit For
                End If

                successfulPurchases +=
                    currentStack - previousStack

            Next

            ' ==================================================
            ' MEMASUKKAN HASIL KE INVENTARIS
            ' ==================================================

            Dim trove As StardewValley.Item =
                TryCast(shop.heldItem, StardewValley.Item)

            If trove IsNot Nothing AndAlso
               trove.QualifiedItemId = TROVE_ID Then

                If Game1.player.addItemToInventoryBool(trove) Then

                    shop.heldItem = Nothing

                Else

                    ' Jika inventaris penuh, jangan buang item.
                    ' Biarkan item tetap berada di kursor toko.

                    Monitor.Log(
                        "Inventory full. Artifact Trove remains held.",
                        LogLevel.Warn
                    )

                End If

            End If

            If successfulPurchases > 0 Then

                Monitor.Log(
                    "Native shop purchases completed: " &
                    successfulPurchases.ToString(),
                    LogLevel.Info
                )

            End If

        Catch ex As Exception

            Monitor.Log(
                "Native shop transaction failed: " &
                ex.ToString(),
                LogLevel.Error
            )

        End Try

    End Sub

End Class
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
                                                    
