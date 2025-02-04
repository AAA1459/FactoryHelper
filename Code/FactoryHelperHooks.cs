﻿using Celeste;
using FactoryHelper.Components;
using FactoryHelper.Cutscenes;
using FactoryHelper.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections;

namespace FactoryHelper {
    public static class FactoryHelperHooks {
        public static void Load() {
            On.Celeste.Player.ctor += ctor;
            On.Celeste.Level.LoadLevel += LoadLevel;
            On.Celeste.Player.Die += PlayerDie;
            On.Celeste.LevelExit.Routine += RespawnRoutine;
            On.Celeste.Player.Pickup += Pickup;
            On.Celeste.Lookout.LookRoutine += LookRoutine;
            On.Celeste.LevelEnter.Go += LevelEnterGo;
            On.Celeste.DashBlock.Break_Vector2_Vector2_bool_bool += DashBlockBreak;
            On.Celeste.DashBlock.RemoveAndFlagAsGone += DashBlockRemoveAndFlagAsGone;
        }

        private static void DashBlockRemoveAndFlagAsGone(On.Celeste.DashBlock.orig_RemoveAndFlagAsGone orig, DashBlock self) {
            if (self is FactoryActivatorDashBlock) {
                self.RemoveSelf();
                return;
            }

            orig(self);
        }

        private static void DashBlockBreak(On.Celeste.DashBlock.orig_Break_Vector2_Vector2_bool_bool orig, DashBlock self, Vector2 from, Vector2 direction, bool playSound, bool playDebrisSound) {
            orig(self, from, direction, playSound, playDebrisSound);
            if (self is FactoryActivatorDashBlock) {
                (self as FactoryActivatorDashBlock).OnBreak();
            }
        }

        private static void LevelEnterGo(On.Celeste.LevelEnter.orig_Go orig, Session session, bool fromSaveData) {
            if (!fromSaveData && session.StartedFromBeginning && session.Area.Mode == AreaMode.Normal && session.Area.ChapterIndex == 1 && session.Area.GetLevelSet() == "KaydenFox/FactoryMod") {
                Engine.Scene = new FactoryIntroVignette(session);
            } else {
                orig(session, fromSaveData);
            }
        }

        private static IEnumerator LookRoutine(On.Celeste.Lookout.orig_LookRoutine orig, Lookout self, Player player) {
            SteamWall steamWall = self.Scene.Tracker.GetEntity<SteamWall>();
            if (steamWall != null) {
                steamWall.Halted = true;
            }

            IEnumerator enumerator = orig(self, player);
            while (enumerator.MoveNext()) {
                yield return enumerator.Current;
            }

            if (steamWall != null) {
                steamWall.Halted = false;
            }
        }

        private static bool Pickup(On.Celeste.Player.orig_Pickup orig, Player self, Holdable pickup) {
            return (self.Holding == null || self.Holding.Entity is not ThrowBox) && orig(self, pickup);
        }

        private static IEnumerator RespawnRoutine(On.Celeste.LevelExit.orig_Routine orig, LevelExit self) {
            FactoryHelperSession factorySession = FactoryHelperModule.Session;
            if (factorySession.SpecialBoxPosition != null) {
                factorySession.OriginalSession.Level = factorySession.SpecialBoxLevel;
                factorySession.OriginalSession.RespawnPoint = factorySession.SpecialBoxPosition;
                Engine.Scene = new LevelLoader(factorySession.OriginalSession);
                factorySession.SpecialBoxPosition = null;
            } else {
                IEnumerator enumerator = orig(self);
                while (enumerator.MoveNext()) {
                    yield return enumerator.Current;
                }
            }
        }

        private static PlayerDeadBody PlayerDie(On.Celeste.Player.orig_Die orig, Player self, Vector2 direction, bool evenIfInvincible, bool registerDeathInStats) {
            Session session = (self.Scene as Level).Session;

            PlayerDeadBody playerDeadBody = orig(self, direction, evenIfInvincible, registerDeathInStats);

            if (playerDeadBody != null) {
                Strawberry goldenStrawb = null;
                foreach (Follower follower in self.Leader.Followers) {
                    if (follower.Entity is Strawberry && (follower.Entity as Strawberry).Golden && !(follower.Entity as Strawberry).Winged) {
                        goldenStrawb = follower.Entity as Strawberry;
                    }
                }

                Vector2? specialBoxLevel = (FactoryHelperModule.Instance._Session as FactoryHelperSession).SpecialBoxPosition;
                if (goldenStrawb == null && specialBoxLevel != null) {
                    playerDeadBody.DeathAction = delegate {
                        Engine.Scene = new LevelExit(LevelExit.Mode.Restart, session);
                    };
                }
            }

            return playerDeadBody;
        }

        private static void LoadLevel(On.Celeste.Level.orig_LoadLevel orig, Level self, Player.IntroTypes playerIntro, bool isFromLoader) {
            orig(self, playerIntro, isFromLoader);
            if (playerIntro != Player.IntroTypes.Transition || isFromLoader) {
                Player player = self.Tracker.GetEntity<Player>();

                foreach (EntityID key in (FactoryHelperModule.Instance._Session as FactoryHelperSession).Batteries) {
                    self.Add(new Battery(player, key));
                }
            }
        }

        private static void ctor(On.Celeste.Player.orig_ctor orig, Player self, Vector2 position, PlayerSpriteMode spriteMode) {
            orig(self, position, spriteMode);
            ConveyorMover conveyorMover = new() {
                OnMove = (amount) => {
                    if (self.StateMachine.State != Player.StClimb) {
                        self.MoveH(amount * Engine.DeltaTime);
                    }
                }
            };
            self.Add(conveyorMover);
        }
    }
}
