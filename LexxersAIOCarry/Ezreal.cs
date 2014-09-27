﻿using System;
using LeagueSharp;
using LeagueSharp.Common;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
    class Ezreal : Champion
    {
        public Ezreal() : base("Ezreal")
        {

        }
        
        protected override void InitializeSpells()
        {
            Spell Q = new Spell(SpellSlot.Q, 1150);
            Q.SetSkillshot(0.25f, 60f, 2000f, true, SkillshotType.SkillshotLine);

            Spell W = new Spell(SpellSlot.W, 1000);
            W.SetSkillshot(0.25f, 80f, 2000f, false, SkillshotType.SkillshotLine);

            Spell E = new Spell(SpellSlot.E, 475);
            E.SetSkillshot(0.25f, 80f, 1600f, false, SkillshotType.SkillshotCircle);

            Spell R = new Spell(SpellSlot.R, float.MaxValue);
            R.SetSkillshot(1.25f, 160f, 2000f, false, SkillshotType.SkillshotLine);

            Spells.Add("Q", Q);
            Spells.Add("W", W);
            Spells.Add("E", E);
            Spells.Add("R", R);
        }

        protected override void CreateMenu()
        {
            Menu.AddSubMenu(new Menu("Combo", "Combo"));
            Menu.SubMenu("Combo").AddItem(new MenuItem("Combo_q", "Use Q").SetValue(true));
            Menu.SubMenu("Combo").AddItem(new MenuItem("Combo_w", "Use W").SetValue(true));

            Menu.AddSubMenu(new Menu("Harass", "Harass"));
            Menu.SubMenu("Harass").AddItem(new MenuItem("Harass_q", "Use Q").SetValue(true));
            Menu.SubMenu("Harass").AddItem(new MenuItem("Harass_w", "Use W").SetValue(false));

            Menu.AddSubMenu(new Menu("Auto", "Auto"));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_q", "Use Q").SetValue(true));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_w", "Use W").SetValue(false));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_r", "Use R").SetValue(true));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_minrange", "Min R range").SetValue(new Slider(1050, 0, 1500)));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_maxrange", "Max R range").SetValue(new Slider(3000, 1500, 5000)));
            Menu.SubMenu("Auto").AddItem(new MenuItem("Auto_manaE", "Keep mana for E").SetValue(true));

            Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("Drawing_q", "Q Range").SetValue(new Circle(true, Color.FromArgb(100, 0, 255, 0))));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("Drawing_w", "W Range").SetValue(new Circle(true, Color.FromArgb(100, 0, 255, 0))));
            Menu.SubMenu("Drawing").AddItem(new MenuItem("Drawing_rdamage", "R Damage Indicator").SetValue(true));
        }

        protected override void Combo()
        {
            if (Menu.Item("Combo_q").GetValue<bool>()) Cast("Q", SimpleTs.DamageType.Physical);
            if (Menu.Item("Combo_w").GetValue<bool>()) Cast("W", SimpleTs.DamageType.Physical);
        }
        protected override void Harass()
        {
            if (Menu.Item("Harass_q").GetValue<bool>() && !(Menu.Item("Auto_manaE").GetValue<bool>() && Player.Spellbook.GetSpell(SpellSlot.E).Level >= 1 && Player.Mana < Player.Spellbook.GetSpell(SpellSlot.E).ManaCost + Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost)) Cast("Q", SimpleTs.DamageType.Physical);
            if (Menu.Item("Harass_w").GetValue<bool>() && !(Menu.Item("Auto_manaE").GetValue<bool>() && Player.Spellbook.GetSpell(SpellSlot.E).Level >= 1 && Player.Mana < Player.Spellbook.GetSpell(SpellSlot.E).ManaCost + Player.Spellbook.GetSpell(SpellSlot.W).ManaCost)) Cast("W", SimpleTs.DamageType.Physical);
        }
        protected override void Auto()
        {
            if (Menu.Item("Auto_q").GetValue<bool>() && !(Menu.Item("Auto_manaE").GetValue<bool>() && Player.Spellbook.GetSpell(SpellSlot.E).Level >= 1 && Player.Mana < Player.Spellbook.GetSpell(SpellSlot.E).ManaCost + Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost)) Cast("Q", SimpleTs.DamageType.Physical);
            if (Menu.Item("Auto_w").GetValue<bool>() && !(Menu.Item("Auto_manaE").GetValue<bool>() && Player.Spellbook.GetSpell(SpellSlot.E).Level >= 1 && Player.Mana < Player.Spellbook.GetSpell(SpellSlot.E).ManaCost + Player.Spellbook.GetSpell(SpellSlot.W).ManaCost)) Cast("W", SimpleTs.DamageType.Physical);
        }
        protected override void Drawing()
        {
            Circle qCircle = Menu.Item("Drawing_q").GetValue<Circle>();
            Circle wCircle = Menu.Item("Drawing_w").GetValue<Circle>();

            if (qCircle.Active)
                Utility.DrawCircle(Player.Position, Spells["Q"].Range, qCircle.Color);
            if (wCircle.Active)
                Utility.DrawCircle(Player.Position, Spells["W"].Range, wCircle.Color);

            Utility.HpBarDamageIndicator.DamageToUnit = UltimateDamage;
            Utility.HpBarDamageIndicator.Enabled = Menu.Item("Drawing_rdamage").GetValue<bool>();
        }
        protected override void Update()
        {
            if (Menu.Item("Auto_r").GetValue<bool>()) CastR();
        } 

        float UltimateDamage(Obj_AI_Hero hero)
        {
            if (!Spells["R"].IsReady())
                return 0;

            float reduction = 1f - ((Spells["R"].GetCollision(Player.Position.To2D(), new List<SharpDX.Vector2> { hero.Position.To2D() }).Count + 2) / 10f);
            if (reduction < 0.3f)
                reduction = 0.3f;

            return (float)DamageLib.getDmg(hero, DamageLib.SpellType.R) * reduction;
        }

        void CastR()
        {
            if (!Spells["R"].IsReady()) return;

            Obj_AI_Hero target = SimpleTs.GetTarget(Menu.Item("Auto_maxrange").GetValue<Slider>().Value, SimpleTs.DamageType.Magical);
            if (target == null || distance(target, Player) < Menu.Item("Auto_minrange").GetValue<Slider>().Value) return;

            float predictedHealth = HealthPrediction.GetHealthPrediction(target, (int)(Spells["R"].Delay + (distance(target, Player) / Spells["R"].Speed) * 100));

            if (UltimateDamage(target) < predictedHealth || predictedHealth <= 0) return;

            if (Spells["R"].GetPrediction(target).Hitchance >= HitChance.High)
                Spells["R"].Cast(target, true);
        }

        float distance(Obj_AI_Hero player, Obj_AI_Hero enemy)
        {
            SharpDX.Vector3 vec = new SharpDX.Vector3();
            vec.X = player.Position.X - enemy.Position.X;
            vec.Y = player.Position.Y - enemy.Position.Y;
            vec.Z = player.Position.Z - enemy.Position.Z;

            return (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y + vec.Z * vec.Z);
        }
    }

}
