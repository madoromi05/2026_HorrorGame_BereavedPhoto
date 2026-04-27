using VContainer;
using VContainer.Unity;

public sealed class CommonLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 参照関係が構築された後に、
        // シーンに存在する全てのオブジェクトにInject(注入)する
        builder.RegisterBuildCallback(resolver =>
        {
            foreach (var rootGameObject in gameObject.scene.GetRootGameObjects())
            {
                resolver.InjectGameObject(rootGameObject);
            }
        });
    }
}