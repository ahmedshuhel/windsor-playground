using System;
using Castle.MicroKernel.Registration;
using Castle.Windsor;
using Xunit;

namespace Play.LifeStyle
{

    public interface IComponent { }
    public class MyComponent : IComponent { }

    public interface IDisposableComponent : IComponent, IDisposable
    {
        bool IsDisposed { get; }
    }

    public class MyDisposableComponent : IDisposableComponent
    {
        public void Dispose()
        {
            IsDisposed = true;
        }

        public bool IsDisposed { get; private set; }
    }

    public interface IService
    {
        IDisposableComponent Component { get; }
    }

    public class Service : IService
    {
        public Service(IDisposableComponent component)
        {
            Component = component;
        }

        public IDisposableComponent Component { get; private set; }
    }


    public class WindsorReleasePolicyTests
    {
        private readonly WindsorContainer _container;

        public WindsorReleasePolicyTests()
        {
            _container = new WindsorContainer();
            _container.Register(Component.For<IComponent>().ImplementedBy<MyComponent>().LifestyleTransient());
            _container.Register(Component.For<IDisposableComponent>().ImplementedBy<MyDisposableComponent>().LifestyleTransient());
            _container.Register(Component.For<IService>().ImplementedBy<Service>().LifestyleTransient());
        }

        [Fact]
        public void ResoleveTransientWithoutDecommissionConcern()
        {
            var c = _container.Resolve<IComponent>();
            Assert.False(_container.Kernel.ReleasePolicy.HasTrack(c));
        }

        [Fact]
        public void ResoleveTransientWithDecommissionConcern()
        {
            var c = _container.Resolve<IDisposableComponent>();
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c));

            _container.Release(c);

            Assert.False(_container.Kernel.ReleasePolicy.HasTrack(c));
            Assert.True(c.IsDisposed);
        }

        [Fact]
        public void Calling_Release_Will_Dispose_The_Disposable_Transient_Component()
        {
            var c = _container.Resolve<IDisposableComponent>();
            _container.Release(c);
            Assert.True(c.IsDisposed);
        }

        [Fact]
        public void Release_Policy_Track_Transient_Commpnent_With_Dependent_Transient_With_Decommision_Concernt()
        {
            var c = _container.Resolve<IService>();
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c));
        }

        [Fact]
        public void Release_Policy_Track_Dependent_Component_Too()
        {
            var c = _container.Resolve<IService>();
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c));
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c.Component));
        }

        [Fact]
        public void After_Calling_Release_Release_Policy_Will_Not_Track_Both_Component_And_Dependent()
        {
            var c = _container.Resolve<IService>();
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c));
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c.Component));

            _container.Release(c);

            Assert.False(_container.Kernel.ReleasePolicy.HasTrack(c));
            Assert.False(_container.Kernel.ReleasePolicy.HasTrack(c.Component));
        }

        [Fact]
        public void After_Calling_Release_Dependent_Will_Be_Disposed_Too()
        {
            var c = _container.Resolve<IService>();
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c));
            Assert.True(_container.Kernel.ReleasePolicy.HasTrack(c.Component));

            _container.Release(c);

            Assert.False(_container.Kernel.ReleasePolicy.HasTrack(c));
            Assert.False(_container.Kernel.ReleasePolicy.HasTrack(c.Component));

            Assert.True(c.Component.IsDisposed);
        }
    }
}
