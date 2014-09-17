using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;
using Color = System.Drawing.Color;

namespace UltimateCarry
{
	class Azir : Champion
	{
		public int SoldierAttackRange = 350;
		public Spell Q;
		public Spell W;
		public Spell E;
		public Spell R;
		public int SoldierCount = 0;

		public Azir()
		{
			LoadMenu();
			LoadSpells();
			Game.OnGameUpdate += Game_OnGameUpdate;
			Drawing.OnDraw += Drawing_OnDraw;
			Game.OnGameSendPacket += Game_OnGameSendPacket;

			PluginLoaded();
		}



		private void LoadSpells()
		{
			
			Q = new Spell(SpellSlot.Q, 800);
			Q.SetSkillshot(0.25f, 50, 800, true, SkillshotType.SkillshotLine );

			W = new Spell(SpellSlot.W, 450);
			W.SetSkillshot(0.20f, 100, float.MaxValue, false, SkillshotType.SkillshotCircle);

			E = new Spell(SpellSlot.E, 1000);
			E.SetSkillshot(0.30f, 80, 800, true, SkillshotType.SkillshotLine);

			R = new Spell(SpellSlot.R, 500);
		}

		private void LoadMenu()
		{
			Program.Menu.AddSubMenu(new Menu("TeamFight", "TeamFight"));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useQ_TeamFight", "Use Q").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useQ_TeamFight_follow", "Follow Q").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight_shield", "W for Shield").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useW_TeamFight_enagage", "W for Engage").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useE_TeamFight", "E to me").SetValue(true));
			Program.Menu.SubMenu("TeamFight").AddItem(new MenuItem("useR_TeamFight", "Use R if Hit").SetValue(new Slider(2, 5, 0)));
			
			Program.Menu.AddSubMenu(new Menu("Harass", "Harass"));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useQ_Harass", "Use Q").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useW_Harass_safe", "W for SafeFriend").SetValue(true));
			Program.Menu.SubMenu("Harass").AddItem(new MenuItem("useE_Harass", "E away").SetValue(true));
			AddManaManager("Harass",50);

			Program.Menu.AddSubMenu(new Menu("LaneClear", "LaneClear"));
			Program.Menu.SubMenu("LaneClear").AddItem(new MenuItem("useE_LaneClear", "Use E").SetValue(true));
			AddManaManager("LaneClear", 20);

			Program.Menu.AddSubMenu(new Menu("SupportMode", "SupportMode"));
			Program.Menu.SubMenu("SupportMode").AddItem(new MenuItem("hitMinions", "Hit Minions").SetValue(false));

			Program.Menu.AddSubMenu(new Menu("Passive", "Passive"));
			Program.Menu.SubMenu("Passive").AddItem(new MenuItem("useQ_Interupt", "Q Interrupt").SetValue(false));
			Program.Menu.SubMenu("Passive").AddItem(new MenuItem("useW_Interupt", "W Interrupt").SetValue(false));

			Program.Menu.AddSubMenu(new Menu("Drawing", "Drawing"));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Disabled", "Disable All").SetValue(false));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_Q", "Draw Q").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_W", "Draw W").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_E", "Draw E").SetValue(true));
			Program.Menu.SubMenu("Drawing").AddItem(new MenuItem("Draw_R", "Draw R").SetValue(true));
		}

		private void Game_OnGameUpdate(EventArgs args)
		{
			GetSoldierCount();
		}


		private void Drawing_OnDraw(EventArgs args)
		{
			foreach(var obj in ObjectManager.Get<Obj_AI_Minion>().Where(obj => obj.Name == "AzirSoldier" && obj.IsAlly && obj.BoundingRadius < 66 && obj.AttackSpeedMod > 1))
			{
				Utility.DrawCircle(obj.Position,SoldierAttackRange ,Color.Blue);
			}
		}

		private void GetSoldierCount()
		{
			SoldierCount =
				ObjectManager.Get<Obj_AI_Base>()
					.Count(obj => obj.Name == "AzirSoldier" && obj.IsAlly && (int) obj.BoundingRadius < 66 && obj.AttackSpeedMod > 1);

		}

		public Vector3 GetMaxSoldierPosition()
		{
			var me = ObjectManager.Player.Position;
			var mouse = Game.CursorPos;

			var newpos = mouse - me;
			newpos.Normalize();
			return me + (newpos * W.Range );
		}



		internal static class Orbwalking
		{
			public delegate void AfterAttackEvenH(Obj_AI_Base unit, Obj_AI_Base target);
			public delegate void BeforeAttackEvenH(BeforeAttackEventArgs args);
			public delegate void OnAttackEvenH(Obj_AI_Base unit, Obj_AI_Base target);

			public enum OrbwalkingMode
			{
				LastHit,
				Mixed,
				LaneClear,
				Combo,
				None,
			}

			private static readonly string[] AttackResets = { "dariusnoxiantacticsonh", "fioraflurry", "garenq", "hecarimrapidslash", "jaxempowertwo", "jaycehypercharge", "leonashieldofdaybreak", "luciane", "lucianq", "monkeykingdoubleattack", "mordekaisermaceofspades", "nasusq", "nautiluspiercinggaze", "netherblade", "parley", "poppydevastatingblow", "powerfist", "renektonpreexecute", "rengarq", "shyvanadoubleattack", "sivirw", "takedown", "talonnoxiandiplomacy", "trundletrollsmash", "vaynetumble", "vie", "volibearq", "xenzhaocombotarget", "yorickspectral" };
			private static readonly string[] NoAttacks = { "jarvanivcataclysmattack", "monkeykingdoubleattack", "shyvanadoubleattack", "shyvanadoubleattackdragon", "zyragraspingplantattack", "zyragraspingplantattack2", "zyragraspingplantattackfire", "zyragraspingplantattack2fire" };
			private static readonly string[] Attacks = { "caitlynheadshotmissile", "frostarrow", "garenslash2", "kennenmegaproc", "lucianpassiveattack", "masteryidoublestrike", "quinnwenhanced", "renektonexecute", "renektonsuperexecute", "rengarnewpassivebuffdash", "trundleq", "xenzhaothrust", "xenzhaothrust2", "xenzhaothrust3" };
			private static readonly List<PassiveDamage> AttackPassives = new List<PassiveDamage>();
			public static int LastAaTick;
			public static bool Attack = true;
			public static bool DisableNextAttack = false;
			public static bool Move = true;
			private static Obj_AI_Base _lastTarget;
			private static readonly Obj_AI_Hero Player;
			static Orbwalking()
			{
				Player = ObjectManager.Player;
				Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
				GameObject.OnCreate += Obj_SpellMissile_OnCreate;
				Game.OnGameProcessPacket += OnProcessPacket;			
			}
			private static void Obj_SpellMissile_OnCreate(GameObject sender, EventArgs args)
			{
				if(sender is Obj_SpellMissile && sender.IsValid)
				{
					var missile = (Obj_SpellMissile)sender;
					if(missile.SpellCaster is Obj_AI_Hero && missile.SpellCaster.IsValid &&
					IsAutoAttack(missile.SData.Name))
					{
						FireAfterAttack(missile.SpellCaster, _lastTarget);
					}
				}
			}

			public static event BeforeAttackEvenH BeforeAttack;

			public static event OnAttackEvenH OnAttack;

			public static event AfterAttackEvenH AfterAttack;
			
			private static void FireBeforeAttack(Obj_AI_Base target)
			{
				if(BeforeAttack != null)
				{
					BeforeAttack(new BeforeAttackEventArgs
					{
						Target = target
					});
				}
				else
				{
					DisableNextAttack = false;
				}
			}
			private static void FireOnAttack(Obj_AI_Base unit, Obj_AI_Base target)
			{
				if(OnAttack != null)
				{
					OnAttack(unit, target);
				}
			}

			private static void FireAfterAttack(Obj_AI_Base unit, Obj_AI_Base target)
			{
				if(AfterAttack != null)
				{
					AfterAttack(unit, target);
				}
			}

			private static float GetAutoAttackPassiveDamage(Obj_AI_Minion minion)
			{
				return
				AttackPassives.Where(
				p => (p.ChampionName == "" || p.ChampionName == Player.ChampionName) && p.IsActive(minion))
				.Sum(passive => passive.GetDamage(minion));
			}

			public static bool IsAutoAttackReset(string name)
			{
				return AttackResets.Contains(name.ToLower());
			}

			public static bool IsMelee( Obj_AI_Base unit)
			{
				return unit.CombatType == GameObjectCombatType.Melee;
			}

			public static bool IsAutoAttack(string name)
			{
				return (name.ToLower().Contains("attack") && !NoAttacks.Contains(name.ToLower())) ||
				Attacks.Contains(name.ToLower());
			}

			public static float GetRealAutoAttackRange(Obj_AI_Base target)
			{
				var result = Player.AttackRange + Player.BoundingRadius;
				if(target != null)
				{
					return result + target.BoundingRadius - (target is Obj_AI_Hero ? 50 : 0);
				}
				return result;
			}

			public static Obj_AI_Hero GetSoldierTargetHero()
			{
				return ObjectManager.Get<Obj_AI_Minion>().Where(obj => obj.Name == "AzirSoldier" && obj.IsAlly && obj.BoundingRadius < 66 && obj.AttackSpeedMod > 1).SelectMany(soldier => ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsValidTarget() && hero.Distance(soldier) < 350)).FirstOrDefault();
			}

			public static bool InAutoAttackRange(Obj_AI_Base target)
			{
				if (target == null) 
					return false;
				var myRange = GetRealAutoAttackRange(target);
				return Vector2.DistanceSquared(target.ServerPosition.To2D(), Player.ServerPosition.To2D()) <=
				       myRange * myRange;
			}

			public static bool InSoldierAttackRange(Obj_AI_Base target)
			{
				return target != null && ObjectManager.Get<Obj_AI_Minion>().Any(obj => obj.Name == "AzirSoldier" && obj.IsAlly && obj.BoundingRadius < 66 && obj.AttackSpeedMod > 1 && obj.Distance(target) < 350);
			}

			public static float GetMyProjectileSpeed()
			{
				return IsMelee(Player) ? float.MaxValue : Player.BasicAttack.MissileSpeed;
			}

			public static bool CanAttack()
			{
				if (LastAaTick <= Environment.TickCount)
					return Environment.TickCount + Game.Ping/2 + 25 >= LastAaTick + Player.AttackDelay*1000 && Attack;
				return false;
			}

			public static bool CanMove(float extraWindup)
			{
				if(LastAaTick <= Environment.TickCount)
				{
					return Environment.TickCount + Game.Ping / 2 >= LastAaTick + Player.AttackCastDelay * 1000 + extraWindup &&
					Move;
				}
				return false;
			}
			private static void MoveTo(Vector3 position, float holdAreaRadius = 0)
			{
				if(Player.ServerPosition.Distance(position) < holdAreaRadius)
				{
					if(Player.Path.Count() > 0)
					{
						Player.IssueOrder(GameObjectOrder.HoldPosition, Player.ServerPosition);
					}
					return;
				}
				var point = Player.ServerPosition +
				400 * (position.To2D() - Player.ServerPosition.To2D()).Normalized().To3D();
				Player.IssueOrder(GameObjectOrder.MoveTo, point);
			}
			public static void Orbwalk(Obj_AI_Base target,
			Vector3 position,
			float extraWindup = 90,
			float holdAreaRadius = 0)
			{
				if(target != null && CanAttack())
				{
					Packet.S2C.HighlightUnit.Encoded(target.NetworkId);
					DisableNextAttack = false;
					FireBeforeAttack(target);
					if(!DisableNextAttack)
					{
						Player.IssueOrder(GameObjectOrder.AttackUnit, target);
						if(!(target is Obj_AI_Hero))
						{
							LastAaTick = Environment.TickCount + Game.Ping / 2;
						}
						Packet.S2C.RemoveHighlightUnit.Encoded(target.NetworkId);
						return;
					}
				}
				if(CanMove(extraWindup))
				{
					MoveTo(position, holdAreaRadius);
				}
			}
			/// <summary>
			/// Resets the Auto-Attack timer.
			/// </summary>
			public static void ResetAutoAttackTimer()
			{
				LastAaTick = 0;
			}
			private static void OnProcessPacket(GamePacketEventArgs args)
			{
				if(args.PacketData[0] == 0x34)
				{
					var stream = new MemoryStream(args.PacketData);
					var b = new BinaryReader(stream);
					b.BaseStream.Position = b.BaseStream.Position + 1;
					var unit = ObjectManager.GetUnitByNetworkId<Obj_AI_Base>(BitConverter.ToInt32(b.ReadBytes(4), 0));
					if(args.PacketData[9] == 17)
					{
						if(unit.IsMe)
						{
							ResetAutoAttackTimer();
						}
					}
				}
			}
			private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs spell)
			{
				if(IsAutoAttackReset(spell.SData.Name) && unit.IsMe)
				{
					Utility.DelayAction.Add(250, ResetAutoAttackTimer);
				}
				if(IsAutoAttack(spell.SData.Name))
				{
					if(unit.IsMe)
					{
						LastAaTick = Environment.TickCount - Game.Ping / 2;
						var aiBase = spell.Target as Obj_AI_Base;
						if(aiBase != null)
						{
							_lastTarget = aiBase;
						}
						if(unit.IsMelee())
						{
							Utility.DelayAction.Add(
							(int)(unit.AttackCastDelay * 1000 + 40), () => FireAfterAttack(unit, _lastTarget));
						}
					}
					FireOnAttack(unit, _lastTarget);
				}
			}
			public class BeforeAttackEventArgs
			{
				public Obj_AI_Base Target;
				public Obj_AI_Base Unit = ObjectManager.Player;
				private bool _process = true;
				public bool Process
				{
					get
					{
						return _process;
					}
					set
					{
						DisableNextAttack = !value;
						_process = value;
					}
				}
			}

			internal class Orbwalker
			{
				private const float LaneClearWaitTimeMod = 2f;
				private readonly Obj_AI_Hero _player;
				private readonly Menu _config;
				private Obj_AI_Base _forcedTarget;
				private Vector3 _orbwalkingPoint;
				private Obj_AI_Minion _prevMinion;
				public Orbwalker(Menu attachToMenu)
				{
					_config = attachToMenu;
					/* Drawings submenu */
					var drawings = new Menu("Drawings", "drawings");
					drawings.AddItem(
					new MenuItem("AACircle", "AACircle").SetShared()
					.SetValue(new Circle(true, Color.FromArgb(255, 255, 0, 255))));
					drawings.AddItem(
					new MenuItem("HoldZone", "HoldZone").SetShared()
					.SetValue(new Circle(false, Color.FromArgb(255, 255, 0, 255))));
					_config.AddSubMenu(drawings);
					/* Misc options */
					var misc = new Menu("Misc", "Misc");
					misc.AddItem(
					new MenuItem("HoldPosRadius", "Hold Position Radius").SetShared().SetValue(new Slider(0, 150, 0)));
					misc.AddItem(
					new MenuItem("PriorizeFarm", "Priorize farm over harass").SetShared().SetValue(true));
					_config.AddSubMenu(misc);
					/* Delay sliders */
					_config.AddItem(
					new MenuItem("ExtraWindup", "Extra windup time").SetShared().SetValue(new Slider(50, 200, 0)));
					_config.AddItem(new MenuItem("FarmDelay", "Farm delay").SetShared().SetValue(new Slider(0, 200, 0)));
					/*Load the menu*/
					_config.AddItem(
					new MenuItem("LastHit", "Last hit").SetShared()
					.SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));
					_config.AddItem(
					new MenuItem("Farm", "Mixed").SetShared()
					.SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));
					_config.AddItem(
					new MenuItem("LaneClear", "LaneClear").SetShared()
					.SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));
					_config.AddItem(
					new MenuItem("Orbwalk", "Combo").SetShared().SetValue(new KeyBind(32, KeyBindType.Press)));
					_player = ObjectManager.Player;
					Game.OnGameUpdate += GameOnOnGameUpdate;
					Drawing.OnDraw += DrawingOnOnDraw;
				}
				private int FarmDelay
				{
					get
					{
						return _config.Item("FarmDelay").GetValue<Slider>().Value;
					}
				}
				public OrbwalkingMode ActiveMode
				{
					get
					{
						if(_config.Item("Orbwalk").GetValue<KeyBind>().Active)
						{
							return OrbwalkingMode.Combo;
						}
						if(_config.Item("LaneClear").GetValue<KeyBind>().Active)
						{
							return OrbwalkingMode.LaneClear;
						}
						if(_config.Item("Farm").GetValue<KeyBind>().Active)
						{
							return OrbwalkingMode.Mixed;
						}
						if(_config.Item("LastHit").GetValue<KeyBind>().Active)
						{
							return OrbwalkingMode.LastHit;
						}
						return OrbwalkingMode.None;
					}
				}

				public void SetAttacks(bool b)
				{
					Attack = b;
				}

				public void SetMovement(bool b)
				{
					Move = b;
				}

				public void ForceTarget(Obj_AI_Base target)
				{
					_forcedTarget = target;
				}

				public void SetOrbwalkingPoint(Vector3 point)
				{
					_orbwalkingPoint = point;
				}
				private bool ShouldWait()
				{
					return
					ObjectManager.Get<Obj_AI_Minion>()
					.Any(
					minion =>
					minion.IsValidTarget() && minion.Team != GameObjectTeam.Neutral &&
					InAutoAttackRange(minion) &&
					HealthPrediction.LaneClearHealthPrediction(
					minion, (int)((_player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay) <=
					DamageLib.CalcPhysicalMinionDmg(
					_player.BaseAttackDamage + _player.FlatPhysicalDamageMod, minion, true) - 1 +
					Math.Max(0, GetAutoAttackPassiveDamage(minion) - 10));
				}
				public Obj_AI_Base GetTarget()
				{
					Obj_AI_Base result = null;
					float[] r = { float.MaxValue };
					if((ActiveMode == OrbwalkingMode.Mixed || ActiveMode == OrbwalkingMode.LaneClear) && !_config.Item("PriorizeFarm").GetValue<bool>())
					{
						var target = SimpleTs.GetTarget(-1, SimpleTs.DamageType.Physical) ?? GetSoldierTargetHero();
						if (target != null)
							return target;
					}

					if(ActiveMode == OrbwalkingMode.LaneClear || ActiveMode == OrbwalkingMode.Mixed ||
					ActiveMode == OrbwalkingMode.LastHit)
					{
						foreach(var minion in
						ObjectManager.Get<Obj_AI_Minion>()
						.Where(minion => minion.IsValidTarget() && (InAutoAttackRange(minion) || InSoldierAttackRange(minion))))
						{
							var t = (int)(_player.AttackCastDelay * 1000) - 100 + Game.Ping / 2 +
							1000 * (int)_player.Distance(minion) / (int)GetMyProjectileSpeed();
							var predHealth = HealthPrediction.GetHealthPrediction(minion, t, FarmDelay);
							if(minion.Team != GameObjectTeam.Neutral && predHealth > 0 &&
							predHealth <=
							DamageLib.CalcPhysicalMinionDmg(
							_player.BaseAttackDamage + _player.FlatPhysicalDamageMod, minion, true) - 1 +
							Math.Max(0, GetAutoAttackPassiveDamage(minion) - 10))
							{
								return minion;
							}
						}
					}
					//Forced target
					if(_forcedTarget != null && _forcedTarget.IsValidTarget() && (InAutoAttackRange(_forcedTarget) || InSoldierAttackRange(_forcedTarget)))
					{
						return _forcedTarget;
					}
					/*Champions*/
					if(ActiveMode != OrbwalkingMode.LastHit)
					{
						var target = SimpleTs.GetTarget(-1, SimpleTs.DamageType.Physical) ?? GetSoldierTargetHero();
						if (target != null)
							return target;
					}
					/*Jungle minions*/
					if(ActiveMode == OrbwalkingMode.LaneClear || ActiveMode == OrbwalkingMode.Mixed)
					{
						foreach(var mob in
						ObjectManager.Get<Obj_AI_Minion>()
						.Where(
						mob =>
						mob.IsValidTarget() && (InAutoAttackRange(mob) || InSoldierAttackRange(mob)) && mob.Team == GameObjectTeam.Neutral)
						.Where(mob => mob.MaxHealth >= r[0] || Math.Abs(r[0] - float.MaxValue) < float.Epsilon))
						{
							result = mob;
							r[0] = mob.MaxHealth;
						}
					}
					if(result != null)
					{
						return result;
					}
					/*Lane Clear minions*/
					r[0] = float.MaxValue;
					if(ActiveMode == OrbwalkingMode.LaneClear)
					{
						if(!ShouldWait())
						{
							if(_prevMinion != null && _prevMinion.IsValidTarget() && (InAutoAttackRange(_prevMinion ) || InSoldierAttackRange(_prevMinion )))
							{
								var predHealth = HealthPrediction.LaneClearHealthPrediction(
								_prevMinion, (int)((_player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay);
								if(predHealth >=
								2 *
								DamageLib.CalcPhysicalMinionDmg(
								_player.BaseAttackDamage + _player.FlatPhysicalDamageMod, _prevMinion, true) - 1 +
								Math.Max(0, GetAutoAttackPassiveDamage(_prevMinion) - 10) ||
								Math.Abs(predHealth - _prevMinion.Health) < float.Epsilon)
								{
									return _prevMinion;
								}
							}
							foreach (var minion in from minion in ObjectManager.Get<Obj_AI_Minion>()
								.Where(minion => minion.IsValidTarget() && (InAutoAttackRange(minion) || InSoldierAttackRange(minion))) let predHealth = HealthPrediction.LaneClearHealthPrediction(
									minion, (int)((_player.AttackDelay * 1000) * LaneClearWaitTimeMod), FarmDelay) where predHealth >=
									                                                                                     2 *
									                                                                                     DamageLib.CalcPhysicalMinionDmg(
										                                                                                     _player.BaseAttackDamage + _player.FlatPhysicalDamageMod, minion, true) - 1 +
									                                                                                     Math.Max(0, GetAutoAttackPassiveDamage(minion) - 10) ||
									                                                                                     Math.Abs(predHealth - minion.Health) < float.Epsilon where minion.Health >= r[0] || Math.Abs(r[0] - float.MaxValue) < float.Epsilon select minion)
							{
								result = minion;
								r[0] = minion.Health;
								_prevMinion = minion;
							}
						}
					}
					/*turrets*/
					if (ActiveMode != OrbwalkingMode.LaneClear) return result;
					foreach(var turret in
						ObjectManager.Get<Obj_AI_Turret>().Where(t => t.IsValidTarget() && InAutoAttackRange(t)))
					{
						return turret;
					}
					return result;
				}

				private void GameOnOnGameUpdate(EventArgs args)
				{
					if(ActiveMode == OrbwalkingMode.None)
					{
						return;
					}
					//Prevent canceling important channeled spells like Miss Fortunes R.
					if(_player.IsChannelingImportantSpell())
					{
						return;
					}
					var target = GetTarget();
					Orbwalk(
					target, (_orbwalkingPoint.To2D().IsValid()) ? _orbwalkingPoint : Game.CursorPos,
					_config.Item("ExtraWindup").GetValue<Slider>().Value,
					_config.Item("HoldPosRadius").GetValue<Slider>().Value);
				}
				private void DrawingOnOnDraw(EventArgs args)
				{
					if(_config.Item("AACircle").GetValue<Circle>().Active)
					{
						Utility.DrawCircle(
						_player.Position, GetRealAutoAttackRange(null) + 65,
						_config.Item("AACircle").GetValue<Circle>().Color);
					}
					if(_config.Item("HoldZone").GetValue<Circle>().Active)
					{
						Utility.DrawCircle(
						_player.Position, _config.Item("HoldPosRadius").GetValue<Slider>().Value,
						_config.Item("HoldZone").GetValue<Circle>().Color);
					}
				}
			}
			internal class PassiveDamage
			{
				public delegate float GetDamageD(Obj_AI_Base minion);
				public delegate bool IsActiveD(Obj_AI_Base minion);
				public string ChampionName = "";
				public GetDamageD GetDamage;
				public IsActiveD IsActive;
			}
		}
	}
	
}
