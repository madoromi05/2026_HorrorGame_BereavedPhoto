using UnityEngine;
using VContainer;
using VContainer.Unity;

public sealed class RootLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.Register<IDebugLogger, UnityDebugLogger>(Lifetime.Singleton);

        builder.RegisterBuildCallback(resolver =>
        {
            foreach (Transform child in transform)
            {
                resolver.InjectGameObject(child.gameObject);
            }
        });
    }
}