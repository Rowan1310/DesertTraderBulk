using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using System.Linq;

namespace DesertTraderBulk
{
    public class ModEntry : Mod
    {
        private Rectangle btn10;
        private Rectangle btnMax;
        private bool showButtons = false;

        public override void Entry(IModHelper helper)
        {
            helper.Events.Display.MenuChanged += OnMenuChanged;
            helper.Events.Display.Rendered += OnRendered;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
        }

        private void OnMenuChanged(object sender, MenuChangedEventArgs e)
        {
            // Cuma aktif kalo buka ShopMenu
            showButtons = e.NewMenu is ShopMenu;
        }

        private void OnRendered(object sender, RenderedEventArgs e)
        {
            if (!showButtons || Game1.activeClickableMenu is not ShopMenu shop) return;
            if (Context.IsWorldReady == false) return;

            // Cek apakah ini Desert Trader: cek item yang dijual ada Artifact Trove
            // Dan cek item yang lagi di-hover / dipilih adalah Artifact Trove
            var hovered = GetHoveredItem(shop);
            if (hovered == null || hovered.Name != "Artifact Trove") return;

            var sb = e.SpriteBatch;
            int x = Game1.uiViewport.Width / 2 - 220;
            int y = 80;

            btn10 = new Rectangle(x, y, 200, 64);
            btnMax = new Rectangle(x + 220, y, 200, 64);

            DrawButton(sb, btn10, "Tukar x10");
            DrawButton(sb, btnMax, "Tukar MAX");
        }

        private void DrawButton(SpriteBatch sb, Rectangle r, string text)
        {
            // Kotak gelap
            sb.Draw(Game1.staminaRect, r, Color.Black * 0.75f);
            // Border kuning
            var font = Game1.dialogueFont;
            Vector2 size = font.MeasureString(text);
            sb.DrawString(font, text, new Vector2(r.X + (r.Width - size.X) / 2, r.Y + (r.Height - size.Y) / 2), Color.Yellow);
        }

        private void OnButtonPressed(object sender, ButtonPressedEventArgs e)
        {
            if (!showButtons) return;
            if (e.Button != SButton.MouseLeft) return;
            if (Game1.activeClickableMenu is not ShopMenu shop) return;

            var hovered = GetHoveredItem(shop);
            if (hovered == null || hovered.Name != "Artifact Trove") return;

            int omni = CountOmni();
            if (omni < 5) return;

            int amount = 0;
            Point p = new Point((int)e.Cursor.ScreenPixels.X, (int)e.Cursor.ScreenPixels.Y);
            if (btn10.Contains(p)) amount = 10;
            else if (btnMax.Contains(p)) amount = omni / 5;
            else return;

            if (amount <= 0) return;
            // Kalo omni gak cukup buat x10, tuker semampunya aja
            if (amount * 5 > omni) amount = omni / 5;
            if (amount <= 0) return;

            // Hapus Omni Geode
            int need = amount * 5;
            RemoveOmni(need);

            // Kasih Artifact Trove
            for (int i = 0; i < amount; i++)
            {
                Game1.player.addItemToInventoryBool(new StardewValley.Object("Artifact Trove", 1));
            }

            Game1.playSound("coin");
            Helper.Input.Suppress(e.Button);
        }

        private Item GetHoveredItem(ShopMenu shop)
        {
            // Di 1.6, item yang di-hover bisa diambil dari forSale + hovered
            // Kita pake cara aman: cek item yang lagi di-hover lewat reflection sederhana
            // Fallback: kalo shop lagi buka dan ada Trove di list, anggap lagi pilih Trove
            // Biar gampang di HP, kita return Trove kalo shop ini jual Trove
            try
            {
                var forSale = shop.forSale;
                var trove = forSale.FirstOrDefault(it => it.Name == "Artifact Trove");
                return trove;
            }
            catch { return null; }
        }

        private int CountOmni()
        {
            int total = 0;
            foreach (var it in Game1.player.Items)
            {
                if (it != null && it.Name == "Omni Geode") total += it.Stack;
            }
            return total;
        }

        private void RemoveOmni(int need)
        {
            for (int i = 0; i < Game1.player.Items.Count && need > 0; i++)
            {
                var it = Game1.player.Items[i];
                if (it != null && it.Name == "Omni Geode")
                {
                    int take = System.Math.Min(it.Stack, need);
                    it.Stack -= take;
                    need -= take;
                    if (it.Stack <= 0) Game1.player.Items[i] = null;
                }
            }
        }
    }
}
