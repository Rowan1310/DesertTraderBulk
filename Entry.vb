
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
    Private Const MAX_PURCHASE_LIMIT As Integer = 999

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

    ' Membaca Stack tanpa memicu konflik nama di VB.NET.
    Private Function GetItemStackCount(
        item As StardewValley.Item
    ) As Integer

        If item Is Nothing Then Return 0

        Try

            Dim members As MemberInfo() =
                item.GetType().GetMember(
                    "Stack",
                    BindingFlags.Instance Or
                    BindingFlags.Public Or
                    BindingFlags.NonPublic
                )

            For Each member As MemberInfo In members

                If TypeOf member Is PropertyInfo Then

                    Dim prop As PropertyInfo =
                        DirectCast(member, PropertyInfo)

                    If prop.GetIndexParameters().Length = 0 Then

                        Return Convert.ToInt32(
                            prop.GetValue(item, Nothing)
                        )

                    End If

                End If

                If TypeOf member Is FieldInfo Then

                    Dim field As FieldInfo =
                        DirectCast(member, FieldInfo)

                    Return Convert.ToInt32(
                        field.GetValue(item)
                    )

                End If

            Next

        Catch ex As Exception

            Monitor.Log(
                "Failed to read item stack: " & ex.Message,
                LogLevel.Warn
            )

        End Try

        Return 0

    End Function

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

    Private Function CountTrove() As Integer

        Dim total As Integer = 0

        If Not Context.IsWorldReady Then Return 0

        For Each item As StardewValley.Item In Game1.player.Items

            If item IsNot Nothing AndAlso
               item.QualifiedItemId = TROVE_ID Then

                total += GetItemStackCount(item)

            End If

        Next

        Dim shop As ShopMenu =
            TryCast(Game1.activeClickableMenu, ShopMenu)

        If shop IsNot Nothing Then

            Dim held As StardewValley.Item =
                TryCast(shop.heldItem, StardewValley.Item)

            If held IsNot Nothing AndAlso
               held.QualifiedItemId = TROVE_ID Then

                total += GetItemStackCount(held)

            End If

        End If

        Return total

    End Function

    Private Function GetMaximumBatch() As Integer

        Return MAX_PURCHASE_LIMIT

    End Function

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

            amount = GetMaximumBatch()

        Else

            Return

        End If

        Helper.Input.Suppress(e.Button)

        Dim held As StardewValley.Item =
            TryCast(shop.heldItem, StardewValley.Item)

        If held IsNot Nothing AndAlso
           held.QualifiedItemId <> TROVE_ID Then

            Game1.playSound("cancel")
            Return

        End If

        If amount <= 0 Then

            Game1.playSound("cancel")
            Return

        End If

        ExchangeThroughNativeShop(
            shop,
            amount
        )

    End Sub

    Private Sub ExchangeThroughNativeShop(
        shop As ShopMenu,
        requestedAmount As Integer
    )

        If shop Is Nothing Then Return

        If Game1.activeClickableMenu IsNot shop Then
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

                    Exit For

                End If

                Dim previousTotal As Integer =
                    CountTrove()

                shop.receiveLeftClick(
                    clickPoint.X,
                    clickPoint.Y,
                    True
                )

                Dim currentTotal As Integer =
                    CountTrove()

                If currentTotal <= previousTotal Then
                    Exit For
                End If

                successfulPurchases +=
                    currentTotal - previousTotal

            Next

            Dim trove As StardewValley.Item =
                TryCast(shop.heldItem, StardewValley.Item)

            If trove IsNot Nothing AndAlso
               trove.QualifiedItemId = TROVE_ID Then

                If Game1.player.addItemToInventoryBool(trove) Then

                    shop.heldItem = Nothing

                Else

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

            Else

                Game1.playSound("cancel")

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
                                            
