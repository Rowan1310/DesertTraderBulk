
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports StardewModdingAPI
Imports StardewModdingAPI.Events
Imports StardewValley
Imports StardewValley.Menus
Imports System.Linq

Public Class Entry
    Inherits [Mod]

    Private btn10 As Rectangle
    Private btnMax As Rectangle
    Private showButtons As Boolean = False

    Public Overrides Sub Entry(helper As IModHelper)

        helper.Events.Display.MenuChanged += AddressOf OnMenuChanged
        helper.Events.Display.Rendered += AddressOf OnRendered
        helper.Events.Input.ButtonPressed += AddressOf OnButtonPressed

        Monitor.Log(
            "Desert Trader Bulk loaded!",
            LogLevel.Info
        )

    End Sub

    Private Sub OnMenuChanged(
        sender As Object,
        e As MenuChangedEventArgs
    )

        showButtons = TypeOf e.NewMenu Is ShopMenu

    End Sub

    Private Sub OnRendered(
        sender As Object,
        e As RenderedEventArgs
    )

        If Not showButtons Then Return

        Dim shop = TryCast(Game1.activeClickableMenu, ShopMenu)

        If shop Is Nothing Then Return

        If Not Context.IsWorldReady Then Return

        Dim hovered = GetHoveredItem(shop)

        If hovered Is Nothing Then Return

        If hovered.Name <> "Artifact Trove" Then Return

        Dim x As Integer = Game1.uiViewport.Width \ 2 - 220
        Dim y As Integer = 80

        btn10 = New Rectangle(x, y, 200, 64)
        btnMax = New Rectangle(x + 220, y, 200, 64)

        DrawButton(e.SpriteBatch, btn10, "Tukar x10")
        DrawButton(e.SpriteBatch, btnMax, "Tukar MAX")

    End Sub

    Private Sub DrawButton(
        sb As SpriteBatch,
        r As Rectangle,
        buttonText As String
    )

        sb.Draw(
            Game1.staminaRect,
            r,
            Color.Black * 0.75F
        )

        Dim font = Game1.dialogueFont

        Dim size As Vector2 = font.MeasureString(buttonText)

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

    Private Sub OnButtonPressed(
        sender As Object,
        e As ButtonPressedEventArgs
    )

        If Not showButtons Then Return

        If e.Button <> SButton.MouseLeft Then Return

        Dim shop = TryCast(Game1.activeClickableMenu, ShopMenu)

        If shop Is Nothing Then Return

        Dim hovered = GetHoveredItem(shop)

        If hovered Is Nothing Then Return

        If hovered.Name <> "Artifact Trove" Then Return

        Dim omni As Integer = CountOmni()

        If omni < 5 Then Return

        Dim amount As Integer = 0

        Dim p As New Point(
            CInt(e.Cursor.ScreenPixels.X),
            CInt(e.Cursor.ScreenPixels.Y)
        )

        If btn10.Contains(p) Then

            amount = 10

        ElseIf btnMax.Contains(p) Then

            amount = omni \ 5

        Else

            Return

        End If

        If amount <= 0 Then Return

        ' Batas berdasarkan jumlah Omni Geode

        amount = Math.Min(amount, omni \ 5)

        ' Tambahkan Artifact Trove terlebih dahulu.
        ' Hanya item yang berhasil masuk yang akan dibayar.

        Dim added As Integer = 0

        For i As Integer = 1 To amount

            Dim trove As New StardewValley.Object(
                "Artifact Trove",
                1
            )

            If Not Game1.player.addItemToInventoryBool(trove) Then
                Exit For
            End If

            added += 1

        Next

        ' Potong Omni Geode sesuai jumlah yang berhasil dibeli.

        If added > 0 Then

            RemoveOmni(added * 5)

            Game1.playSound("coin")

        End If

        Helper.Input.Suppress(e.Button)

    End Sub

    Private Function GetHoveredItem(
        shop As ShopMenu
    ) As StardewValley.Item

        Try

            Dim trove = shop.forSale.FirstOrDefault(
                Function(it) it IsNot Nothing AndAlso
                it.Name = "Artifact Trove"
            )

            Return TryCast(trove, StardewValley.Item)

        Catch ex As Exception

            Monitor.Log(
                "Unable to detect Artifact Trove: " & ex.Message,
                LogLevel.Warn
            )

            Return Nothing

        End Try

    End Function

    Private Function CountOmni() As Integer

        Dim total As Integer = 0

        For Each item In Game1.player.Items

            If item IsNot Nothing AndAlso
               item.Name = "Omni Geode" Then

                total += item.Stack

            End If

        Next

        Return total

    End Function

    Private Sub RemoveOmni(need As Integer)

        For i As Integer = 0 To Game1.player.Items.Count - 1

            If need <= 0 Then Exit For

            Dim item = Game1.player.Items(i)

            If item IsNot Nothing AndAlso
               item.Name = "Omni Geode" Then

                Dim take As Integer = Math.Min(
                    item.Stack,
                    need
                )

                item.Stack -= take
                need -= take

                If item.Stack <= 0 Then
                    Game1.player.Items(i) = Nothing
                End If

            End If

        Next

    End Sub

End Class
                      
