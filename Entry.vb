
Imports Microsoft.Xna.Framework
Imports Microsoft.Xna.Framework.Graphics
Imports StardewModdingAPI
Imports StardewModdingAPI.Events
Imports StardewValley
Imports StardewValley.Menus
Imports System.Linq
Imports System.Reflection

Public Class Entry
    Inherits [Mod]

    Private btn10 As Rectangle
    Private btnMax As Rectangle
    Private showButtons As Boolean = False

    Public Overrides Sub Entry(helper As IModHelper)

        AddHandler helper.Events.Display.MenuChanged,
            AddressOf OnMenuChanged

        AddHandler helper.Events.Display.Rendered,
            AddressOf OnRendered

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

        showButtons = TypeOf e.NewMenu Is ShopMenu

    End Sub

    Private Sub OnRendered(
        sender As Object,
        e As RenderedEventArgs
    )

        If Not showButtons Then Return
        If Not Context.IsWorldReady Then Return

        Dim shop As ShopMenu =
            TryCast(Game1.activeClickableMenu, ShopMenu)

        If shop Is Nothing Then Return

        Dim hovered As StardewValley.Item =
            GetHoveredItem(shop)

        If hovered Is Nothing Then Return

        If hovered.Name <> "Artifact Trove" Then Return

        Dim x As Integer =
            Game1.uiViewport.Width \ 2 - 220

        Dim y As Integer = 80

        btn10 = New Rectangle(x, y, 200, 64)
        btnMax = New Rectangle(x + 220, y, 200, 64)

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

    Private Sub OnButtonPressed(
        sender As Object,
        e As ButtonPressedEventArgs
    )

        If Not showButtons Then Return

        If e.Button <> SButton.MouseLeft Then Return

        Dim shop As ShopMenu =
            TryCast(Game1.activeClickableMenu, ShopMenu)

        If shop Is Nothing Then Return

        Dim hovered As StardewValley.Item =
            GetHoveredItem(shop)

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

        amount = Math.Min(amount, omni \ 5)

        If amount <= 0 Then Return

        Dim added As Integer = 0

        ' Tambahkan trove terlebih dahulu.
        ' Pembayaran hanya dilakukan untuk item yang berhasil masuk.

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

        If added > 0 Then

            RemoveOmni(added * 5)

            Game1.playSound("coin")

            Monitor.Log(
                "Successfully exchanged " &
                added.ToString() &
                " Artifact Trove.",
                LogLevel.Info
            )

        End If

        Helper.Input.Suppress(e.Button)

    End Sub

    Private Function GetHoveredItem(
        shop As ShopMenu
    ) As StardewValley.Item

        Try

            Dim trove = shop.forSale.FirstOrDefault(
                Function(it)
                    Return it IsNot Nothing AndAlso
                           it.Name = "Artifact Trove"
                End Function
            )

            Return TryCast(
                trove,
                StardewValley.Item
            )

        Catch ex As Exception

            Monitor.Log(
                "Unable to detect Artifact Trove: " &
                ex.Message,
                LogLevel.Warn
            )

            Return Nothing

        End Try

    End Function

    ' Mengakses Stack melalui Reflection.
    ' Ini menghindari konflik nama anggota Stack di VB.NET.

    Private Function GetItemStack(
        item As StardewValley.Item
    ) As Integer

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

        Dim stackField As FieldInfo =
            itemType.GetField(
                "stack",
                BindingFlags.Public Or
                BindingFlags.NonPublic Or
                BindingFlags.Instance
            )

        If stackField IsNot Nothing Then

            Return CInt(stackField.GetValue(item))

        End If

        Throw New MissingMemberException(
            "Unable to find item stack member."
        )

    End Function

    Private Sub SetItemStack(
        item As StardewValley.Item,
        value As Integer
    )

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

        Dim stackField As FieldInfo =
            itemType.GetField(
                "stack",
                BindingFlags.Public Or
                BindingFlags.NonPublic Or
                BindingFlags.Instance
            )

        If stackField IsNot Nothing Then

            stackField.SetValue(item, value)
            Return

        End If

        Throw New MissingMemberException(
            "Unable to set item stack member."
        )

    End Sub

    Private Function CountOmni() As Integer

        Dim total As Integer = 0

        For Each item In Game1.player.Items

            If item IsNot Nothing AndAlso
               item.Name = "Omni Geode" Then

                total += GetItemStack(item)

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
                                        
