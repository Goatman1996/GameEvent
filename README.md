# GameEvent 基于[Attribute]的Unity事件系统
GameEvent 2.0

一款基于C#属性[Attribute]的Unity事件系统

一款优雅的事件解决方案


### 2.0 版本  新增内容

支持了<泛型>事件

支持单一事件的订阅/取消订阅

代码优化，性能应该比1.0版本有明显提升(懒得测试了),

### 基本用法演示
``` csharp
using GameEvent;

// 定义一个叫SomeEvt的事件
public struct SomeEvt : IGameEvent
{

}

// 订阅事件
[GameEvent]
private void OnSomeEvt(SomeEvt evt)
{
    Debug.Log("SomeEvt happened");
}

private void SomeMethod()
{
    // 发起 SomeEvt 事件
    var evt = new SomeEvt();
    evt.Invoke();
}

// 手动监听/取消监听
private void ManullySample()
{
    // 手动监听/取消监听API，方法上可以不用打标签，打标签只是一个自动监听标记
    GameEventDriver.RegisterEvent<T>(Action<T> target) where T : IGameEvent
    GameEventDriver.UnregisterEvent<T>(Action<T> target) where T : IGameEvent    
    GameEventDriver.RegisterTask<T>(Func<T, Task> target) where T : IGameTask
    GameEventDriver.UnregisterTask<T>(Func<T, Task> target) where T : IGameTask
    
    // 示例
    GameEventDriver.UnRegisterEvent<SomeEvt>(OnSomeEvt);
}
```
以上的例子中，三个部分，是可以在任意位置的

## 实现原理
使用Mono.Cecil，对程序集进行注入

由于是注入代码的关系，对类似HybridCLR（huatuo）等实现C# dll热更的解决方案，是原生支持的

## 安装
https://openupm.cn/packages/com.gm.gameevent/

## 配置
Unity中打开

ProjectSettings/GameEventSettings

![GameEvtSettings](https://github.com/Goatman1996/GameEvent/assets/48623605/e3f8da1e-5ce7-4d8f-ae5e-059ec4ec1a1a)

### Assembly List
添加包含【事件定义】及【事件使用】的程序集名称

### Need Injected Log
勾选后，会在每次编译注入完成后，打印所有事件的使用日志

![GameEvtLog](https://github.com/Goatman1996/GameEvent/assets/48623605/e6c2313a-a961-44a6-8ee6-495f95be145e)

### 重新编译脚本 按钮
在修改了Assembly List后，点击，可及时重新编译脚本，

## 使用
### 初始化
```csharp
// 初始化API
GameEvent.GameEventDriver.Initialize(string assemblyName, bool throwOnError);

// 在任何事件发起前
// 在对应程序集加载后
// 对GameEventSettings.AssemblyList中填写的程序集，进行初始化
GameEvent.GameEventDriver.Initialize("Assembly-CSharp", true);
GameEvent.GameEventDriver.Initialize("OtherAssembly", true);
...
```
### 定义事件
事件分为两种，一种是同步事件，一种是异步事件

继承IGameEvent或IGameTask，即可定义事件

定义事件的类型，可以是struct，可以是class，随意选择

1，同步事件
```csharp
public struct SyncEvt : GameEvent.IGameEvent
{
    // 事件信息可自定义（可以没有）
    // 事件参数1
    public int param1;
    public GameObject param2;
    ...
}
```
2，异步事件
```csharp
public struct AsyncEvt : GameEvent.IGameTask
{
    // 事件信息部分同上
}
```

### 订阅事件
[GameEvent.GameEvent]属性

参数 (bool CallOnlyIfMonoEnable = false) 意为，当该函数的所属对象是MonoBehaviour时，会额外判断，MonoBehaviour是否Enable，Enable=true时才会被触发事件

~~注：订阅事件，是以对象为单位的，即某个对象的订阅和取消订阅，会将对象上的所有Game Event，统一订阅或取消~~  

2.0版本可以自由的订阅和取消单一事件

1，同步事件

在任何返回值为void的函数上，添加[GameEvent]属性

且参数是对应的事件类型本身，即为订阅事件

非静态函数，必须是属于class对象的，struct中的非静态函数，不能订阅事件

```csharp
public class FooObject
{
    [GameEvent]
    private void OnSyncEvt(SyncEvt evt)
    {
        Debug.Log("OnSyncEvt");
    }
    
    [GameEvent]
    private static void OnSyncEvt_Static(SyncEvt evt)
    {
        Debug.Log("OnSyncEvt_Static");
    }
}
```
2，异步事件

在任何返回值为Task(或Task<>)的函数上，添加[GameEvent]属性

且参数是对应的事件类型本身，即为订阅事件

```csharp
public class FooObject
{
    [GameEvent]
    private async Task OnAsyncEvt(AsyncEvt evt)
    {
        Debug.Log("OnAsyncEvt");
    }
    
    [GameEvent]
    private static Task OnAsyncEvt_Static(AsyncEvt evt)
    {
        Debug.Log("OnAsyncEvt_Static");
    }
}
```
以上的事件订阅均是自动发生的

3，手动订阅(2.0新增)
``` csharp
// IGameEvent
GameEventDriver.RegisterEvent<T>(Action<T> target) where T : IGameEvent
// IGameTask
GameEventDriver.RegisterTask<T>(Func<T, Task> target) where T : IGameTask
```


### 取消订阅

如订阅者为MonoBehaviour，则在销毁后，自动取消订阅

如订阅者为非MonoBehaviour的对象，则需要手动取消订阅

手动取消订阅API(2.0新增)
``` csharp
// IGameEvent
GameEventDriver.UnregisterEvent<T>(Action<T> target) where T : IGameEvent
// IGameTask
GameEventDriver.UnregisterTask<T>(Func<T, Task> target) where T : IGameTask
```

### 发布事件

1，同步事件
```csharp
using GameEvent;

var evt = new SyncEvt();
evt.Invoke();
```
2，异步事件
```csharp
using GameEvent;

var evt = new AsyncEvt();
await evt.InvokeTask();
```
### 泛型事件支持(2.0新增)
现在事件支持泛型
```csharp
using GameEvent;

// 泛型事件
public struct GenericEvt<T> : IGameEvent
{
    public T Value;
}

// 监听确定类型的泛型事件
[GameEvent]
private void OnStringGenericEvt(GenericEvt<string> evt)
{
    Debug.Log($"OnStringGenericEvt Message {evt.Value}");
}

// 调起泛型事件
private void Invoke=GenericEvt()
{
    // 调起<string>事件
    var stringEvt = new GenericEvt<string>() { Value = "Hello" };
    stringEvt.Invoke();

    // 调起<int>事件
    var intEvt = new GenericEvt<int>() { Value = 1 };
    intEvt.Invoke();
}
```

### 打包相关

如果使用了类似HybridCLR（huatuo）等实现C# dll热更的解决方案，
则需要自行在打包后，对程序集进行注入

API
```csharp
GameEvent.GlobalEventInjecter.InjectEvent(string dir, params string[] dllFileArray)
// dir 为程序集目录，如 "./Library/ScriptAssemblies"
// dllFileArray 为 GameEventSettings.AssemblyList 中填写的程序集名称
```

### 性能

测试 10000000 千万次 的调起事件（Editor环境中，空方法）

``` csharp
[仅使用GameEvent]
OnlyNullEvt : 87ms
[使用GameEvent(true)]
WithEnableEvt : 369ms
[在非MonoBehaviour中使用GameEvent]
NotMonoCall : 41ms
[普通的反射]
Reflection : 5441ms
[直接调起方法]
NativeCall : 4ms
[借用Action，调起方法]
ActionCall : 14ms
```
简单解析一下，在MonoBehaviour中使用Game Event，每次都需要检查MonoBehaviour是否为空

而UnityEngine.Object的判空，是经过一系列复杂操作的，所以大部分耗时都在这里

[在非MonoBehaviour中使用GameEvent]这一栏，算是纯净的消耗，耗时只有[借用Action，调起方法]的不到3倍，可以接受

而[使用GameEvent(true)]这一栏，由于还要在判断MonoBehaviour是否Enable，所以耗时大大提升

*注意这是千万级，一般使用完全不会成为性能阻碍

如果认为性能是瓶颈，目前只有一个简单的优化方案

为MonoBehaviour重写判空

```csharp
// 在某MonoBehaviour中写如下代码

private bool IsAlive = true;
// Does the object exist?
public static implicit operator bool(MyMono exists)
{
    return exists.IsAlive;
}

private void OnDestroy()
{
    IsAlive = false;
}
```
经过上述优化后，新的测试结果如下
``` csharp
[仅使用GameEvent]
OnlyNullEvt : 48ms
[使用GameEvent(true)]
WithEnableEvt : 339ms
[在非MonoBehaviour中使用GameEvent]
NotMonoCall : 41ms
[普通的反射]
Reflection : 5441ms
[直接调起方法]
NativeCall : 4ms
[借用Action，调起方法]
ActionCall : 14ms
```

如果还是觉得性能有问题的话
就.....

### 最后

觉得有趣的点个Star~

谢谢~
