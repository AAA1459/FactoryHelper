﻿using Celeste;
using Celeste.Mod.Entities;
using FactoryHelper.Components;
using Microsoft.Xna.Framework;
using Monocle;
using System.Collections.Generic;

namespace FactoryHelper.Triggers {
    [CustomEntity("FactoryHelper/PermanentActivationTrigger")]
    public class PremanentActivationTrigger : Trigger {
        private readonly FactoryActivator[] _activators;
        private readonly HashSet<string> _shouldStayPermanent = new();
        private Level _level;

        public PremanentActivationTrigger(EntityData data, Vector2 offset) 
            : base(data, offset) {
            string[] _activationIds = data.Attr("activationIds").Split(',');
            _activators = new FactoryActivator[_activationIds.Length];
            for (int i = 0; i < _activationIds.Length; i++) {
                Add(_activators[i] = new FactoryActivator());
                _activators[i].ActivationId = _activationIds[i];
            }
        }

        public override void Added(Scene scene) {
            base.Added(scene);
            _level = Scene as Level;
            if (_activators.Length == 0) {
                RemoveSelf();
            }

            foreach (FactoryActivator activator in _activators) {
                activator.HandleStartup(scene);
                if (activator.Activated) {
                    _shouldStayPermanent.Add(activator.ActivationId);
                }
            }
        }

        public override void OnEnter(Player player) {
            base.OnEnter(player);
            foreach (FactoryActivator activator in _activators) {
                if (activator.Activated) {
                    _level.Session.SetFlag($"FactoryActivation:{activator.ActivationId}", true);
                }
            }
        }

        public override void OnLeave(Player player) {
            if (!CollideCheck(player)) {
                foreach (FactoryActivator activator in _activators) {
                    if (!_shouldStayPermanent.Contains(activator.ActivationId)) {
                        _level.Session.SetFlag($"FactoryActivation:{activator.ActivationId}", false);
                    }
                }
            }

            base.OnLeave(player);
        }
    }
}
