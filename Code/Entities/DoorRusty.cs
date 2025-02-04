﻿using Celeste;
using Celeste.Mod.Entities;
using Microsoft.Xna.Framework;
using Monocle;
using System;

namespace FactoryHelper.Entities {
    [CustomEntity("FactoryHelper/DoorRusty")]
    [Tracked(false)]
    public class DoorRusty : Actor {
        private readonly Sprite sprite;
        private readonly string openSfx;
        private readonly string closeSfx;
        private readonly LightOcclude occlude;
        private bool disabled;

        public DoorRusty(EntityData data, Vector2 offset)
            : base(data.Position + offset) {
            Depth = 8499;
            Add(sprite = FactoryHelperModule.SpriteBank.Create("rusty_metal_door"));
            openSfx = "event:/game/03_resort/door_metal_open";
            closeSfx = "event:/game/03_resort/door_metal_close";
            sprite.Play("idle");
            Collider = new Hitbox(12f, 22f, -6f, -23f);
            Add(occlude = new LightOcclude(new Rectangle(-1, -24, 2, 24)));
            Add(new PlayerCollider(HitPlayer));
        }

        public override bool IsRiding(Solid solid) {
            return Scene.CollideCheck(new Rectangle((int)X - 2, (int)Y - 2, 4, 4), solid);
        }

        protected override void OnSquish(CollisionData data) { }

        private void HitPlayer(Player player) {
            if (!disabled) {
                Open(player.X);
            }
        }

        public void Open(float x) {
            if (sprite.CurrentAnimationID == "idle") {
                Audio.Play(openSfx, Position);
                sprite.Play("open");
                if (X != x) {
                    sprite.Scale.X = Math.Sign(x - X);
                }
            } else if (sprite.CurrentAnimationID == "close") {
                sprite.Play("close", restart: true);
            }
        }

        public override void Update() {
            string currentAnimationID = sprite.CurrentAnimationID;
            base.Update();
            occlude.Visible = sprite.CurrentAnimationID == "idle";
            if (!disabled && CollideCheck<Solid>()) {
                disabled = true;
            }

            if (currentAnimationID == "close" && sprite.CurrentAnimationID == "idle") {
                Audio.Play(closeSfx, Position);
            }
        }
    }
}
