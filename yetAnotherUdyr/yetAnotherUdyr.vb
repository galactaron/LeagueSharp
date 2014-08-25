﻿Imports LeagueSharp
Imports LeagueSharp.Common
Imports LeagueSharp.Common.TargetSelector
Imports System.Linq
Imports SharpDX
Imports System.Drawing
Imports System.Collections.Generic

Public Class yetAnotherUdyr
	'Script Information
	Shared versionNumber As String = "1.1.0"

	'Ease of use
	Shared player As Obj_AI_Hero = ObjectManager.Player

	Shared Config As Menu
	Shared Orbwalker As Orbwalking.Orbwalker

	Shared levelUpListPhoenix As New List(Of Integer) From {3, 2, 1, 0, 3, 3, 1, 3, 1, 3, 1, 1, 2, 2, 2, 2, 0, 0}
	Shared levelUpListTiger As New List(Of Integer) From {0, 2, 1, 0, 0, 2, 0, 1, 0, 2, 2, 2, 1, 1, 1, 3, 3, 3}

	'Spells
	Shared Q As Spell
	Shared W As Spell
	Shared E As Spell
	Shared R As Spell

	'ignite
	Shared igniteSpell As SpellSlot
	Shared hasIgnite As Boolean
	Shared ignite As New Igniter.Ignite

	'smite
	Shared smiteSpell As SpellSlot
	Shared hasSmite As Boolean
	Shared smiteList As New List(Of String) From {"Dragon", "Worm", "AncientGolem", "LizardElder", "GiantWolf", "Wraith"}
	'Items
	''Offensive - minus 25 range
	Shared BilgeCut As New Items.Item(3144, 475)
	Shared BoTRK As New Items.Item(3153, 425)
	Shared RavHydra As New Items.Item(3074, 375)
	Shared Tiamat As New Items.Item(3077, 375)
	Shared RanOmen As New Items.Item(3143, 490)
	''Defensive - minus 10 range
	Shared LoTIS As New Items.Item(3190, 590)

	'Drawing
	Shared enemyColor As New Dictionary(Of Integer, System.Drawing.Color)

	Public Shared Sub Main(ByVal arg() As String)
		AddHandler CustomEvents.Game.OnGameLoad, AddressOf Game_onGameLoad
	End Sub

#Region "Event Handlers"

	Public Shared Sub Game_onGameLoad(args As System.EventArgs)

		Game.PrintChat("yetAnotherUdyr by FlapperDoodle, version: " & versionNumber)
		If Not ObjectManager.Player.ChampionName = "Udyr" Then
			Game.PrintChat("Please use Udyr~")
			Return
		End If

		'Spell Initialize
		Q = New Spell(SpellSlot.Q, 200)
		W = New Spell(SpellSlot.W, 200)
		E = New Spell(SpellSlot.E, 200)
		R = New Spell(SpellSlot.R, 200)

		'Main Menu
		Config = New Menu("yA-Udyr", "yA-Udyr", True)

		'Orbwalker
		Config.AddSubMenu(New Menu("Orbwalking", "Orbwalking"))
		Orbwalker = New Orbwalking.Orbwalker(Config.SubMenu("Orbwalking"))

		'Target Selector
		Dim targetSelector = New Menu("Target Selector", "Target Selector")
		SimpleTs.AddToMenu(targetSelector)
		Config.AddSubMenu(targetSelector)

		'Main
		Config.AddItem(New MenuItem("Combo Key", "Combo Key").SetValue(New KeyBind(32, KeyBindType.Press)))
		Config.AddItem(New MenuItem("Style", "Style").SetValue(New StringList({"Phoenix", "Tiger"}, 0)))

		'Items
		Config.AddSubMenu(New Menu("Items", "Items"))
		''Offensive
		Config.SubMenu("Items").AddSubMenu(New Menu("Offense", "Offense"))
		Config.SubMenu("Items").SubMenu("Offense").AddItem(New MenuItem("BilgeCut", "Bilgewater Cutlass").SetValue(True))
		Config.SubMenu("Items").SubMenu("Offense").AddItem(New MenuItem("BoTRK", "BoT Ruined King").SetValue(True))
		Config.SubMenu("Items").SubMenu("Offense").AddItem(New MenuItem("RavHydra", "Ravenous Hydra").SetValue(True))
		Config.SubMenu("Items").SubMenu("Offense").AddItem(New MenuItem("RanOmen", "Randuin's Omen").SetValue(True))
		Config.SubMenu("Items").SubMenu("Offense").AddItem(New MenuItem("Tiamat", "Tiamat").SetValue(True))
		''Defensive
		Config.SubMenu("Items").AddSubMenu(New Menu("Defense", "Defense"))
		Config.SubMenu("Items").SubMenu("Defense").AddSubMenu(New Menu("LoT Iron Solari", "LoTIS-Menu"))
		'''LoT-IS
		Config.SubMenu("Items").SubMenu("Defense").SubMenu("LoTIS-Menu").AddItem(New MenuItem("LoTIS", "Enabled").SetValue(True))
		Config.SubMenu("Items").SubMenu("Defense").SubMenu("LoTIS-Menu").AddItem(New MenuItem("LoTIS-HP-%", "Use at HP %").SetValue(New Slider(40)))

		'Farm
		Config.AddSubMenu(New Menu("Farm", "Farm"))
		Config.SubMenu("Farm").AddItem(New MenuItem("Use-Q-Farm", "Use Q").SetValue(True))
		Config.SubMenu("Farm").AddItem(New MenuItem("Use-R-Farm", "Use R").SetValue(True))
		Config.SubMenu("Farm").AddItem(New MenuItem("Farm-Mana", "Mana Limit").SetValue(New Slider(20)))
		Config.SubMenu("Farm").AddItem(New MenuItem("Farm Key", "Farm Key").SetValue(New KeyBind(86, KeyBindType.Press)))

		'Jungle Farm
		Config.AddSubMenu(New Menu("Jungle Farm", "Jungle Farm"))
		Config.SubMenu("Jungle Farm").AddItem(New MenuItem("Use-Q-Jungle", "Use Q").SetValue(True))
		Config.SubMenu("Jungle Farm").AddItem(New MenuItem("Use-R-Jungle", "Use R").SetValue(True))
		Config.SubMenu("Jungle Farm").AddItem(New MenuItem("Use-W-Jungle", "Use W").SetValue(True))
		Config.SubMenu("Jungle Farm").AddItem(New MenuItem("Jungle-Mana", "Mana Limit").SetValue(New Slider(20)))
		Config.SubMenu("Jungle Farm").AddItem(New MenuItem("Jungle Farm Key", "Jungle Farm Key").SetValue(New KeyBind(67, KeyBindType.Press)))

		'Misc
		Config.AddSubMenu(New Menu("Misc", "Misc"))
		Config.SubMenu("Misc").AddItem(New MenuItem("Auto Level", "Auto Level").SetValue(True))
		Config.SubMenu("Misc").AddItem(New MenuItem("Stun Lock", "Stun Lock").SetValue(True))

		''Ignite
		igniteSpell = player.GetSpellSlot("SummonerDot")

		If igniteSpell = SpellSlot.Unknown Then
			Game.PrintChat("yA-Udyr: Ignite-related functions disabled.")
		Else
			Config.SubMenu("Misc").AddSubMenu(New Menu("Ignite", "Ignite"))
			Config.SubMenu("Misc").SubMenu("Ignite").AddItem(New MenuItem("Auto Ignite", "Auto Ignite").SetValue(New StringList({"In Combo", "Killsteal", "Killable", "Off"}, 0)))
			Config.SubMenu("Misc").SubMenu("Ignite").AddItem(New MenuItem("Ignite HP", "Ignite in Combo, HP %").SetValue(New Slider(35)))
			AddHandler ignite.CanKillEnemies, AddressOf CanKillEnemiesIgnite
			AddHandler ignite.CanKillstealEnemies, AddressOf CanKillStealEnemiesIgnite
			hasIgnite = True
		End If

		''Smite
		smiteSpell = player.GetSpellSlot("SummonerSmite")

		If smiteSpell = SpellSlot.Unknown Then
			Game.PrintChat("yA-Udyr: Smite-related functions disabled.")
		Else
			Config.SubMenu("Misc").AddSubMenu(New Menu("Smite", "Smite"))
			Config.SubMenu("Misc").SubMenu("Smite").AddItem(New MenuItem("Auto Smite", "Auto Smite").SetValue(New StringList({"All", "Buffs", "D/B", "Buffs/D/B", "Off"}, 0)))
			hasSmite = True
		End If

		'Drawing
		Config.AddSubMenu(New Menu("Drawing", "Drawing"))
		Config.SubMenu("Drawing").AddItem(New MenuItem("Draw", "Draw").SetValue(True))
		''Enemy Status
		For Each enemy As Obj_AI_Hero In From hero In ObjectManager.Get(Of Obj_AI_Hero)() Where hero.IsEnemy
			'Add the enemy to our dictionary, so we can update the colors
			enemyColor.Add(enemy.NetworkId, System.Drawing.Color.Green)
			'Assign a menu to each enemy
			Config.SubMenu("Drawing").AddSubMenu(New Menu(enemy.ChampionName, enemy.ChampionName))
			Config.SubMenu("Drawing").SubMenu(enemy.ChampionName).AddItem(New MenuItem(enemy.NetworkId & "E", "Enabled").SetValue(True))
			Config.SubMenu("Drawing").SubMenu(enemy.ChampionName).AddItem(New MenuItem(enemy.NetworkId & "KC", "Killable Circle").SetValue(True))
			Config.SubMenu("Drawing").SubMenu(enemy.ChampionName).AddItem(New MenuItem(enemy.NetworkId & "HP", "HP").SetValue(True))
			Config.SubMenu("Drawing").SubMenu(enemy.ChampionName).AddItem(New MenuItem(enemy.NetworkId & "MP", "MP").SetValue(True))
			Config.SubMenu("Drawing").SubMenu(enemy.ChampionName).AddItem(New MenuItem(enemy.NetworkId & "R", "Range").SetValue(New StringList({"Basic", "Q", "W", "E", "R"})))
			Config.SubMenu("Drawing").SubMenu(enemy.ChampionName).AddItem(New MenuItem(enemy.NetworkId & "RC", "Range Color").SetValue(New Circle(True, System.Drawing.Color.Gray)))
		Next
		Config.AddToMainMenu()

		'Handles
		AddHandler Game.OnGameUpdate, AddressOf Game_OnGameUpdate
		AddHandler Drawing.OnDraw, AddressOf Drawing_OnDraw
		AddHandler CustomEvents.Unit.OnLevelUp, AddressOf Unit_OnLevelUp
		AddHandler Orbwalking.BeforeAttack, AddressOf Orbwalking_BeforeAttack
	End Sub

	'Orbwalk Events Here, farm goes here
	Shared Sub Orbwalking_BeforeAttack(args As Orbwalking.BeforeAttackEventArgs)
		'if farm key is pressed
		If Config.Item("Farm Key").GetValue(Of KeyBind).Active AndAlso Orbwalking.CanMove(50) AndAlso ((player.Mana / player.MaxMana) * 100) >= Config.Item("Farm-Mana").GetValue(Of Slider).Value Then Farm()
	End Sub

	'Auto level up skill
	Shared Sub Unit_OnLevelUp(sender As Obj_AI_Base, args As CustomEvents.Unit.OnLevelUpEventArgs)
		If sender.IsMe AndAlso Config.Item("Auto Level").GetValue(Of Boolean)() Then
			If Config.Item("Style").GetValue(Of StringList)().SelectedIndex = 0 Then
				player.Spellbook.LevelUpSpell(levelUpListPhoenix(args.NewLevel - 1))
			Else
				player.Spellbook.LevelUpSpell(levelUpListTiger(args.NewLevel - 1))
			End If
		End If
	End Sub

	'If KillableEnemies are in range and the config are on
	Shared Sub CanKillEnemiesIgnite(sender As Object, args As Igniter.IgniteEventArgs)
		If Config.Item("Auto Ignite").GetValue(Of StringList)().SelectedIndex = 2 Then ignite.Cast(args.Enemies(0))
	End Sub

	Shared Sub CanKillStealEnemiesIgnite(sender As Object, args As Igniter.IgniteEventArgs)
		If Config.Item("Auto Ignite").GetValue(Of StringList)().SelectedIndex = 1 Or Config.Item("Auto Ignite").GetValue(Of StringList)().SelectedIndex = 2 Then ignite.Cast(args.Enemies(0))
	End Sub

	Shared Sub Game_OnGameUpdate(args As EventArgs)
		If player.IsDead Then Return
		'If combo key is pressed
		If Config.Item("Combo Key").GetValue(Of KeyBind).Active Then ComboIt()

		'Smite
		If hasSmite = True AndAlso Not Config.Item("Auto Smite").GetValue(Of StringList)().SelectedIndex = 3 AndAlso player.SummonerSpellbook.CanUseSpell(smiteSpell) = SpellState.Ready Then CastSmite()

		'Defensive Items (currently only LoTIS)
		If Config.Item("LoTIS").GetValue(Of Boolean)() AndAlso LoTIS.IsReady AndAlso ((player.Health / player.MaxHealth) * 100) <= Config.Item("LoTIS-HP-%").GetValue(Of Slider).Value() Then LoTIS.Cast(player)

		'If jungle key is pressed
		If Config.Item("Jungle Farm Key").GetValue(Of KeyBind).Active AndAlso ((player.Mana / player.MaxMana) * 100) >= Config.Item("Jungle-Mana").GetValue(Of Slider).Value Then JungleFarm()

		'If drawing is on
		If Config.Item("Draw").GetValue(Of Boolean)() Then UpdateIsKillable()
	End Sub

#End Region

#Region "Methods/Functions"

	Shared Sub ComboIt()

		'Create target
		Dim target = SimpleTs.GetTarget(600.0F, SimpleTs.DamageType.Magical)

		If target Is Nothing Then Return

		'If auto-ignite is set to "In Combo" or "Both" mode
		If hasIgnite AndAlso player.SummonerSpellbook.CanUseSpell(igniteSpell) = SpellState.Ready Then
			Dim configIgnite = Config.Item("Auto Ignite").GetValue(Of StringList)().SelectedIndex
			If configIgnite = 2 AndAlso ignite.CanKill(target) Then
				ignite.Cast(target)
			ElseIf configIgnite = 0 AndAlso Config.Item("Ignite HP").GetValue(Of Slider).Value >= ((target.Health / target.MaxHealth) * 100) Then
				ignite.Cast(target)
			End If
		End If

		'Skill order sequence
		If player.Distance(target) < 300 Then
			'If stun lock is on, the target doesn't have a stun buff, and the spell is ready, then cast bear stun
			If Config.Item("Stun Lock").GetValue(Of Boolean)() AndAlso E.IsReady() AndAlso Not target.HasBuff("udyrbearstuncheck", True) Then E.Cast()

			If Config.Item("Style").GetValue(Of StringList)().SelectedIndex = 0 Then
				If R.IsReady() Then R.Cast()
				If Q.IsReady() Then Q.Cast()
				If E.IsReady AndAlso Not target.HasBuff("udyrbearstuncheck", True) Then E.Cast()
				If W.IsReady() Then W.Cast()
			Else
				If E.IsReady AndAlso Not target.HasBuff("udyrbearstuncheck", True) Then E.Cast()
				If Q.IsReady() Then Q.Cast()
				If W.IsReady() Then W.Cast()
				If R.IsReady() Then R.Cast()
			End If
		End If

		'Do Attack Items
		If Config.Item("BoTRK").GetValue(Of Boolean)() AndAlso BoTRK.IsReady Then BoTRK.Cast(target)
		If Config.Item("RavHydra").GetValue(Of Boolean)() AndAlso RavHydra.IsReady Then RavHydra.Cast(target)
		If Config.Item("BilgeCut").GetValue(Of Boolean)() AndAlso BilgeCut.IsReady Then BilgeCut.Cast(target)
		If Config.Item("Tiamat").GetValue(Of Boolean)() AndAlso Tiamat.IsReady Then Tiamat.Cast(target)
		If Config.Item("RanOmen").GetValue(Of Boolean)() AndAlso RanOmen.IsReady AndAlso player.Distance(target) <= 490 Then RanOmen.Cast(target)
	End Sub

	Shared Sub Farm()
		Dim minions = MinionManager.GetMinions(player.ServerPosition, 500.0F)
		If minions.Count > 2 Then
			If Config.Item("Use-R-Farm").GetValue(Of Boolean)() AndAlso R.IsReady Then R.Cast()
			If Config.Item("Use-Q-Farm").GetValue(Of Boolean)() AndAlso Q.IsReady() Then Q.Cast()
		End If
	End Sub

	Shared Sub JungleFarm()

		Dim jungleMobs = MinionManager.GetMinions(player.ServerPosition, 700, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth)
		If jungleMobs.Count = 0 Then Return

		If Config.Item("Use-R-Jungle").GetValue(Of Boolean)() AndAlso R.IsReady() Then R.Cast()
		If Config.Item("Use-Q-Jungle").GetValue(Of Boolean)() AndAlso Q.IsReady() Then Q.Cast()
		If Config.Item("Use-W-Jungle").GetValue(Of Boolean)() AndAlso W.IsReady() Then W.Cast()
	End Sub

	Shared Sub CastSmite()
		Dim mobs = MinionManager.GetMinions(player.ServerPosition, 900.0F, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth)
		If mobs.Count = 0 Then Return
		'Initialize 
		Dim playerLevel = player.Level
		'Get smite damage
		Dim smiteDamage = Math.Max(20 * playerLevel + 370, Math.Max(30 * playerLevel + 330, Math.Max(40 * playerLevel + 240, 50 * playerLevel + 100)))
		For Each jMob In From mob In mobs Where smiteList.Contains(mob.SkinName) AndAlso mob.IsValidTarget(790) AndAlso smiteDamage >= mob.Health
			Select Case Config.Item("Auto Smite").GetValue(Of StringList)().SelectedIndex
				Case 0
					player.SummonerSpellbook.CastSpell(smiteSpell, jMob)
				Case 1
					If jMob.SkinName = "AncientGolem" OrElse jMob.SkinName = "LizardElder" Then player.SummonerSpellbook.CastSpell(smiteSpell, jMob)
				Case 2
					If jMob.SkinName = "Worm" OrElse jMob.SkinName = "Dragon" Then player.SummonerSpellbook.CastSpell(smiteSpell, jMob)
				Case 3
					If jMob.SkinName = "AncientGolem" OrElse jMob.SkinName = "LizardElder" OrElse jMob.SkinName = "Dragon" OrElse jMob.SkinName = "Worm" Then player.SummonerSpellbook.CastSpell(smiteSpell, jMob)
			End Select
		Next
	End Sub

	Shared Sub UpdateIsKillable()
		For Each enemy In From hero In ObjectManager.Get(Of Obj_AI_Hero)() Where hero.IsEnemy AndAlso hero.IsValidTarget

			'Regular
			Dim totalDamage As Double = DamageLib.getDmg(enemy, DamageLib.SpellType.AD, DamageLib.StageType.Default)

			'Damage Spells
			If Q.IsReady Then totalDamage += DamageLib.getDmg(enemy, DamageLib.SpellType.Q)
			If R.IsReady Then totalDamage += DamageLib.getDmg(enemy, DamageLib.SpellType.R)

			'Items
			If BilgeCut.IsReady AndAlso Config.Item("BilgeCut").GetValue(Of Boolean)() Then totalDamage += DamageLib.getDmg(enemy, DamageLib.SpellType.BILGEWATER)
			If BoTRK.IsReady AndAlso Config.Item("BoTRK").GetValue(Of Boolean)() Then totalDamage += DamageLib.getDmg(enemy, DamageLib.SpellType.BOTRK)
			If RavHydra.IsReady AndAlso Config.Item("RavHydra").GetValue(Of Boolean)() Then totalDamage += DamageLib.getDmg(enemy, DamageLib.SpellType.HYDRA)
			If Tiamat.IsReady AndAlso Config.Item("Tiamat").GetValue(Of Boolean)() Then totalDamage += DamageLib.getDmg(enemy, DamageLib.SpellType.TIAMAT)

			Select Case ((enemy.Health - totalDamage) / enemy.MaxHealth)
				Case Is >= 0.66
					enemyColor(enemy.NetworkId) = System.Drawing.Color.Green
				Case 0.329 To 0.66
					enemyColor(enemy.NetworkId) = System.Drawing.Color.Yellow
				Case Is <= 0.329
					enemyColor(enemy.NetworkId) = System.Drawing.Color.Red
			End Select
		Next
	End Sub

#End Region

#Region "Drawing"

	Shared Sub Drawing_OnDraw(args As EventArgs)
		If Not Config.Item("Draw").GetValue(Of Boolean)() Then Return

		For Each enemyVisible As Obj_AI_Hero In From enemy In ObjectManager.Get(Of Obj_AI_Hero)() Where enemy.IsValidTarget AndAlso Config.Item(enemy.NetworkId & "E").GetValue(Of Boolean)()

			'Regular drawing
			If Config.Item(enemyVisible.NetworkId & "KC").GetValue(Of Boolean)() Then Utility.DrawCircle(enemyVisible.Position, 60, enemyColor(enemyVisible.NetworkId), 2, 15)
			If Config.Item(enemyVisible.NetworkId & "HP").GetValue(Of Boolean)() Then Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)(0) - 40, Drawing.WorldToScreen(enemyVisible.Position)(1) - 100, System.Drawing.Color.Red, Convert.ToInt32(enemyVisible.Health / enemyVisible.MaxHealth * 100) & "%")
			If Config.Item(enemyVisible.NetworkId & "MP").GetValue(Of Boolean)() Then Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)(0) + 10, Drawing.WorldToScreen(enemyVisible.Position)(1) - 100, System.Drawing.Color.Violet, Convert.ToInt32(enemyVisible.Mana / enemyVisible.MaxMana * 100) & "%")

			'Range of Skills
			If Not Config.Item(enemyVisible.NetworkId & "RC").GetValue(Of Circle).Active Then Continue For
			'If color is off, then go to the next enemy that is visible
			Select Case Config.Item(enemyVisible.NetworkId & "R").GetValue(Of StringList).SelectedIndex
				Case 0
					Utility.DrawCircle(enemyVisible.Position, enemyVisible.BasicAttack.CastRange(0),Config.Item(enemyVisible.NetworkId & "RC").GetValue(Of Circle).Color, 1, 22)
				Case 1
					Utility.DrawCircle(enemyVisible.Position, enemyVisible.Spellbook.GetSpell(SpellSlot.Q).SData.CastRange(0), Config.Item(enemyVisible.NetworkId & "RC").GetValue(Of Circle).Color, 1, 22)
				Case 2
					Utility.DrawCircle(enemyVisible.Position, enemyVisible.Spellbook.GetSpell(SpellSlot.W).SData.CastRange(0),Config.Item(enemyVisible.NetworkId & "RC").GetValue(Of Circle).Color, 1, 22)
				Case 3
					Utility.DrawCircle(enemyVisible.Position, enemyVisible.Spellbook.GetSpell(SpellSlot.E).SData.CastRange(0),Config.Item(enemyVisible.NetworkId & "RC").GetValue(Of Circle).Color, 1, 22)
				Case 4
					Utility.DrawCircle(enemyVisible.Position, enemyVisible.Spellbook.GetSpell(SpellSlot.R).SData.CastRange(0),Config.Item(enemyVisible.NetworkId & "RC").GetValue(Of Circle).Color, 1, 22)
			End Select
		Next
	End Sub

#End Region
End Class