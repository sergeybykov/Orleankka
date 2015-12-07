﻿using System;

namespace Orleankka.Core
{
    public interface IActorActivator
    {
        void Init(object properties);

        Actor Activate(Type type, string id, IActorRuntime runtime);
    }

    public abstract class ActorActivator<TProperties> : IActorActivator
    {
        void IActorActivator.Init(object properties)
        {
            Init((TProperties) properties);
        }

        public abstract void Init(TProperties properties);
        public abstract Actor Activate(Type type, string id, IActorRuntime runtime);
    }

    public abstract class ActorActivator : ActorActivator<object>
    {
        public override void Init(object properties) {}
    }

    class DefaultActorActivator : ActorActivator
    {
        public override Actor Activate(Type type, string id, IActorRuntime runtime)
        {
            try
            {
                return (Actor) Activator.CreateInstance(type, nonPublic: true);
            }
            catch (MissingMethodException)
            {
                throw new InvalidOperationException(
                    $"No parameterless constructor defined for {type} type");
            }
        }
    }

    static class ActorActivatorExtensions
    {
        public static Actor Activate(this IActorActivator activator, ActorPath path, IActorRuntime runtime)
        {
            var type = ActorType.Registered(path.Code);

            return activator.Activate(type.Implementation, path.Id, runtime);
        }
    }
}
