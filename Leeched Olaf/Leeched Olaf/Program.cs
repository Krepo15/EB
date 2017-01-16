using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Font = SharpDX.Direct3D9.Font;
using SharpDX.Direct3D9;
using Color = System.Drawing.Color;

namespace Leeched_Olaf
{
    class Program
    {
        private static Menu olafMenu, comboMenu, waveClear, LastHit, Flee, Drawings, harass;
        internal class Axe
        {
            public GameObject Object { get; set; }
            public float NetworkId { get; set; }
            public Vector3 AxePos { get; set; }
            public double ExpireTime { get; set; }
        }
        static void Main(string[] args)
        {
            Loading.OnLoadingComplete += Loading_OnLoadingComplete;

        }


        private static void Game_OnTick(EventArgs args)
        {
            if (Orbwalker.ActiveModesFlags.Equals(Orbwalker.ActiveModes.Combo))
            {
                Combo();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LastHit))
            {
                LastHitFunc();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                FleeFunc();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.LaneClear))
            {
                WaveClearFunc();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Harass))
            {
                HarassFunc();
            }
            if (Orbwalker.ActiveModesFlags.HasFlag(Orbwalker.ActiveModes.Flee))
            {
                FleeFunc();
            }
        }

        private static AIHeroClient User = Player.Instance;
        private static Spell.Skillshot A;
        private static Spell.Active Z;
        private static Spell.Targeted E;
        private static Spell.Active R;
        private static readonly Axe oAxe = new Axe();
        public static int LastTickTime;
        private static void Loading_OnLoadingComplete(EventArgs args)
        {

            //Declaring
            Game.OnTick += Game_OnTick;
            Drawing.OnDraw += Drawing_OnDraw;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
            A = new Spell.Skillshot(SpellSlot.Q, 1000, skillShotType: SkillShotType.Linear, castDelay: 250, spellSpeed: 1550, spellWidth: 75);
            Z = new Spell.Active(SpellSlot.W);
            E = new Spell.Targeted(SpellSlot.E, 325);
            R = new Spell.Active(SpellSlot.R);
            if (User.ChampionName != "Olaf")
            {
                return;
            }
            //Declaring


            //Drawings
            olafMenu = MainMenu.AddMenu("Olaf", "Olaf");
            comboMenu = olafMenu.AddSubMenu("Combo");
            comboMenu.Add("A", new CheckBox("Use Q"));
            comboMenu.Add("Z", new CheckBox("Use W"));
            comboMenu.Add("E", new CheckBox("Use E"));
            waveClear = olafMenu.AddSubMenu("Wave Clear / Jungle Clear");
            waveClear.Add("Awc", new CheckBox("Use Q"));
            waveClear.Add("Wwc", new CheckBox("Use W"));
            waveClear.Add("Ewc", new CheckBox("Use E"));
            waveClear.Add("manawc", new Slider("Mana manager", 0));
            LastHit = olafMenu.AddSubMenu("Last hit");
            LastHit.Add("Alh", new CheckBox("Use Q to Last hit"));
            LastHit.Add("Elh", new CheckBox("Use E to Last hit"));
            LastHit.Add("manalh", new Slider("Mana manager", 0));
            harass = olafMenu.AddSubMenu("Harass hit");
            harass.Add("AHarass", new CheckBox("Use Q to Harass"));
            harass.Add("EHarass", new CheckBox("Use E to Harass"));
            harass.Add("manaharass", new Slider("Mana manager", 0));
            Flee = olafMenu.AddSubMenu("Flee");
            Flee.Add("Aflee", new CheckBox("Use Q to Flee"));
            Flee.Add("Rflee", new CheckBox("Use R to Flee (Cast when under CC)"));
            Flee.Add("HPflee", new Slider("Use R when under XX HP", 15));
            Drawings = olafMenu.AddSubMenu("Drawings", "Drawings");
            Drawings.AddGroupLabel("Drawing Settings");
            Drawings.Add("QDraw", new CheckBox("Draw Q Range"));
            Drawings.Add("EDraw", new CheckBox("Draw E Range", false));
            Drawings.Add("Axepos", new CheckBox("Draw Axe position"));

            Chat.Print("Leeched Olaf loaded successfully");
        }
        private static void Drawing_OnDraw(EventArgs args)
        {
            if (Drawings["QDraw"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Orange, BorderWidth = 1, Radius = A.Range }.Draw(User.Position);
            }

            if (Drawings["EDraw"].Cast<CheckBox>().CurrentValue)
            {
                new Circle() { Color = Color.Orange, BorderWidth = 1, Radius = E.Range }.Draw(User.Position);
            }
            var exTime = TimeSpan.FromSeconds(oAxe.ExpireTime - Game.Time).TotalSeconds;
            var color = exTime > 4 ? System.Drawing.Color.Yellow : System.Drawing.Color.Red;
            if (Drawings["Axepos"].Cast<CheckBox>().CurrentValue)
            {
                if (oAxe.Object != null)
                {
                    new Circle() { Color = Color.GreenYellow, BorderWidth = 6, Radius = 100 }.Draw(oAxe.Object.Position);
                    var line = new Geometry.Polygon.Line(
                    User.Position,
                    oAxe.AxePos,
                    User.Distance(oAxe.AxePos));
                    line.Draw(color, 2);
                }
            }
        }
        //Drawings

        //Combo        
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(A.Range, DamageType.Physical);
            if (target == null) return;

            if (comboMenu["A"].Cast<CheckBox>().CurrentValue)
            {
                if (A.IsReady() && target.IsValidTarget(A.Range))
                {
                    var castPosition = A.GetPrediction(target).CastPosition.Extend(Player.Instance.Position, -100);

                    if (User.Distance(target) > 300)
                    {
                        A.Cast(castPosition.To3DWorld());
                    }
                    else
                    {
                        A.Cast(A.GetPrediction(target).CastPosition);
                    }
                }
            }

            if (comboMenu["Z"].Cast<CheckBox>().CurrentValue)
            {
                if (Z.IsReady() && target.IsValidTarget(E.Range))
                {
                    Z.Cast();
                }
            }
            if (comboMenu["E"].Cast<CheckBox>().CurrentValue)
            {
                if (E.IsReady() && User.Distance(target.ServerPosition) <= E.Range)
                {
                    E.Cast(target);
                }
            }
        }
        //Combo

        //LastHitting
        private static void LastHitFunc()
        {
            var cstohit = EntityManager.MinionsAndMonsters.GetLaneMinions().Where(a => a.Distance(Player.Instance) <= A.Range).OrderBy(a => a.Health).FirstOrDefault();
            if (EntityManager.MinionsAndMonsters.GetLaneMinions().Where(a => a.Distance(Player.Instance) <= A.Range).OrderBy(a => a.Health).FirstOrDefault() != null)
            {
                if (LastHit["Alh"].Cast<CheckBox>().CurrentValue && A.IsReady() && Player.Instance.ManaPercent > LastHit["manalh"].Cast<Slider>().CurrentValue && cstohit.IsValidTarget(A.Range) && Player.Instance.GetSpellDamage(cstohit, SpellSlot.Q) >= cstohit.TotalShieldHealth())
                {
                    A.Cast(cstohit);
                }

                if (LastHit["Elh"].Cast<CheckBox>().CurrentValue && E.IsReady() && Player.Instance.GetSpellDamage(cstohit, SpellSlot.E) >= cstohit.TotalShieldHealth())
                {
                    E.Cast(cstohit);
                }
            }
        }
        //LastHitting

        //Harass
        private static void HarassFunc()
        {
            var mana = harass["manaharass"].Cast<Slider>().CurrentValue;
            var target = TargetSelector.GetTarget(A.Range, DamageType.Physical);
            if (target == null) return;

            if (harass["AHarass"].Cast<CheckBox>().CurrentValue)
            {
                if (A.IsReady() && target.IsValidTarget(A.Range) && Player.Instance.ManaPercent > mana)
                {
                    var castPosition = A.GetPrediction(target).CastPosition.Extend(Player.Instance.Position, -100);

                    if (User.Distance(target) > 300)
                    {
                        A.Cast(castPosition.To3DWorld());
                    }
                    else
                    {
                        A.Cast(A.GetPrediction(target).CastPosition);
                    }
                }
            }
            if (harass["EHarass"].Cast<CheckBox>().CurrentValue)
            {
                if (E.IsReady() && target.IsValidTarget(E.Range))
                {
                    E.Cast(target);
                }
            }
        }
        //harass

        //Flee
        private static void FleeFunc()
        {
            var target = TargetSelector.GetTarget(A.Range, DamageType.Physical);
            if (target == null) return;
            var dangerHP = Flee["HPflee"].Cast<Slider>().CurrentValue;
            if (Flee["Aflee"].Cast<CheckBox>().CurrentValue)
            {
                if (A.IsReady() && target.IsValidTarget(A.Range))
                {
                    var castPosition = A.GetPrediction(target).CastPosition.Extend(Player.Instance.Position, -100);

                    if (User.Distance(target) > 300)
                    {
                        A.Cast(castPosition.To3DWorld());
                    }
                    else
                    {
                        A.Cast(A.GetPrediction(target).CastPosition);
                    }
                }
            }
            if (Flee["Rflee"].Cast<CheckBox>().CurrentValue)
            {
                if (R.IsReady() && Player.HasBuffOfType(BuffType.Knockback) || Player.HasBuffOfType(BuffType.Blind) || Player.HasBuffOfType(BuffType.Charm) || Player.HasBuffOfType(BuffType.Silence) || Player.HasBuffOfType(BuffType.Suppression) || Player.HasBuffOfType(BuffType.Sleep) || Player.HasBuffOfType(BuffType.Polymorph) || Player.HasBuffOfType(BuffType.Frenzy) || Player.HasBuffOfType(BuffType.Disarm) || Player.HasBuffOfType(BuffType.Poison) || Player.HasBuffOfType(BuffType.Stun) || Player.HasBuffOfType(BuffType.Taunt) || Player.HasBuffOfType(BuffType.Fear) || Player.HasBuffOfType(BuffType.Slow) || Player.HasBuffOfType(BuffType.Knockup) || Player.HasBuffOfType(BuffType.NearSight) || User.IsRooted && Player.Instance.HealthPercent <= dangerHP)
                {
                    R.Cast();
                }
            }
        }
        //Flee

        //WaveClear
        private static void WaveClearFunc()
        {

            var monsters = EntityManager.MinionsAndMonsters.GetJungleMonsters(User.Position, A.Range).OrderByDescending(a => a.MaxHealth).FirstOrDefault();
            var cstohit = EntityManager.MinionsAndMonsters.GetLaneMinions().Where(a => a.Distance(Player.Instance) <= A.Range).OrderBy(a => a.Health).FirstOrDefault();
            if (monsters != null)
            {
                if (waveClear["Awc"].Cast<CheckBox>().CurrentValue && A.IsReady() && A.IsInRange(monsters) && Player.Instance.ManaPercent >= waveClear["manawc"].Cast<Slider>().CurrentValue)
                {
                    A.Cast(monsters);
                }
                if (waveClear["Wwc"].Cast<CheckBox>().CurrentValue && Z.IsReady() && monsters.IsValidTarget(325) && Player.Instance.ManaPercent >= waveClear["manawc"].Cast<Slider>().CurrentValue)
                {
                    Z.Cast();
                }

                if (waveClear["Ewc"].Cast<CheckBox>().CurrentValue && E.IsReady() && E.IsInRange(monsters))
                {
                    E.Cast(monsters);
                }
            }
            if (cstohit != null)
            {
                if (waveClear["Awc"].Cast<CheckBox>().CurrentValue && A.IsReady() && A.IsInRange(cstohit) && Player.Instance.ManaPercent >= waveClear["manawc"].Cast<Slider>().CurrentValue)
                {
                    var objAiHero = from x1 in ObjectManager.Get<Obj_AI_Minion>()
                                    where x1.IsValidTarget() && x1.IsEnemy
                                    select x1
                    into h
                                    orderby h.Distance(User) descending
                                    select h
                        into x2
                                    where x2.Distance(User) < A.Range - 20 && !x2.IsDead
                                    select x2;
                    var aiMinions = objAiHero as Obj_AI_Minion[] ?? objAiHero.ToArray();
                    var lastMinion = aiMinions.First();
                    A.Cast(lastMinion.Position);
                }
                if (waveClear["Wwc"].Cast<CheckBox>().CurrentValue && Z.IsReady() && cstohit.IsValidTarget(325) && Player.Instance.ManaPercent >= waveClear["manawc"].Cast<Slider>().CurrentValue)
                {
                    Z.Cast();
                }

                if (waveClear["Ewc"].Cast<CheckBox>().CurrentValue && E.IsReady() && E.IsInRange(cstohit))
                {
                    E.Cast(cstohit);
                }
            }
        }
        //WaveClear

        //Axe parameters
        private static void GameObject_OnCreate(GameObject obj, EventArgs args)
        {

            if (obj.Name.ToLower().Contains("olaf_base_q_axe") && obj.Name.ToLower().Contains("ally"))
            {
                oAxe.Object = obj;
                oAxe.ExpireTime = Game.Time + 8;
                oAxe.NetworkId = obj.NetworkId;
                oAxe.AxePos = obj.Position;
            }
        }

        private static void GameObject_OnDelete(GameObject obj, EventArgs args)
        {
            if (obj.Name.ToLower().Contains("olaf_base_q_axe") && obj.Name.ToLower().Contains("ally"))
            {
                oAxe.Object = null;
                LastTickTime = 0;
            }
        }
        //Axe parameters

    }
}
