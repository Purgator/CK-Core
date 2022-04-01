using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CK.Core
{
    /// <summary>
    /// Exposes the identity of the current application (technically the Application Domain) as an immutable
    /// singleton <see cref="Instance"/>.
    /// <see cref="Configure(Action{Builder})"/> or <see cref="TryConfigure(Action{Builder})"/> methods can be called
    /// until the instance is used. <see cref="OnInitialized(Action)"/> enables deferring actions to wait for the application
    /// identity to be ready.
    /// <para>
    /// The "party" is identified by the <see cref="DomainName"/>, <see cref="EnvironmentName"/> and <see cref="PartyName"/>.
    /// This "party" is the logical process: logs issued by a process can always be grouped by this "party identifier".
    /// </para>
    /// But "process uniqueness" is a complex matter. A process can only be truly unique (at any point in time) by using
    /// synchronization primitives like <see cref="System.Threading.Mutex"/> or specific coordination infrastructure
    /// (election algorithms) in distributed systems.
    /// <para>
    /// At least, the "running instance" itself is necessarily unique because of the <see cref="InstanceId"/>.
    /// </para>
    /// <para>
    /// Other "identity" can be captured by the <see cref="ContextIdentifier"/>: this can use process arguments, working directory
    /// or any other contextual information that helps identify a process. Whether this uniquely identifies the process
    /// is not (and cannot) be handled by this model.
    /// </para>
    /// </summary>
    public sealed partial class CoreApplicationIdentity
    {
        /// <summary>
        /// Gets the name of the domain to which this application belongs.
        /// It cannot be null or empty and defaults to "Undefined". This reserved name
        /// must prevent any logs to be sent to any collector that is not on the machine
        /// that runs this application (this is typically used on developer's machine).
        /// <para>
        /// It must be a case sensitive identifier that should use PascalCase convention:
        /// it must only contain 'A'-'Z', 'a'-'z', '0'-'9' and '_' characters and must not
        /// start with a digit nor a '_'.
        /// </para>
        /// </summary>
        public string DomainName { get; }

        /// <summary>
        /// Gets the name of the environment. Defaults to the empty string.
        /// <para>
        /// When not empty, it must be a case sensitive identifier that should use PascalCase convention:
        /// it must only contain 'A'-'Z', 'a'-'z', '0'-'9' and '_' characters and must not
        /// start with a digit nor a '_'.
        /// </para>
        /// </summary>
        public string EnvironmentName { get; }

        /// <summary>
        /// Gets this party name.
        /// <para>
        /// It must be a case sensitive identifier that should use PascalCase convention:
        /// it must only contain 'A'-'Z', 'a'-'z', '0'-'9' and '_' characters and must not
        /// start with a digit nor a '_'.
        /// </para>
        /// <para>
        /// Defaults to a string derived from the <see cref="Environment.ProcessPath"/>.
        /// If the ProcessPath is null, the "Undefined" string is used.
        /// </para>
        /// </summary>
        public string PartyName { get; }

        /// <summary>
        /// Gets a string that identifies the context into which this
        /// application is running.
        /// <para>
        /// There is no constraint on this string but shorter is better.
        /// </para>
        /// <para>
        /// Defaults to the empty string.
        /// </para>
        /// </summary>
        public string ContextIdentifier { get; }

        /// <summary>
        /// Gets an opaque random string that identifies this running instance.
        /// </summary>
        public string InstanceId { get; }

        CoreApplicationIdentity( Builder b )
        {
            DomainName = b.DomainName;
            EnvironmentName = b.EnvironmentName;
            PartyName = b.PartyName ?? "Undefined";
            ContextIdentifier = b.ContextIdentifier ?? "";
            InstanceId = b.InstanceId;
        }

        static Builder? _builder;
        static readonly CancellationTokenSource _token;
        static CoreApplicationIdentity? _instance;

        static CoreApplicationIdentity()
        {
            _builder = new Builder();
            _token = new CancellationTokenSource();
        }

        /// <summary>
        /// Configure the application identity if it's not yet initialized or throws an <see cref="InvalidOperationException"/> otherwise.
        /// </summary>
        /// <param name="configurator">The configuration action.</param>
        public static void Configure( Action<Builder> configurator )
        {
            lock( _token )
            {
                if( _builder == null ) Throw.InvalidOperationException( "CoreApplicationIdentity is already initialized." );
                else configurator( _builder );
            }
        }

        /// <summary>
        /// Tries to configure the application identity if it's not yet initialized.
        /// </summary>
        /// <param name="configurator">The configuration action.</param>
        /// <returns>True if the <paramref name="configurator"/> has been called, false if the <see cref="Instance"/> is already available.</returns>
        public static bool TryConfigure( Action<Builder> configurator )
        {
            lock( _token )
            {
                if( _builder != null )
                {
                    configurator( _builder );
                    return true;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets whether the <see cref="Instance"/> has been initialized or
        /// can still be configured.
        /// </summary>
        public static bool IsInitialized => _token.IsCancellationRequested;

        /// <summary>
        /// Registers a callback that will be called when the <see cref="Instance"/> will be available
        /// or immediately if the instance has already been configured.
        /// </summary>
        /// <param name="action">Any action that requires the application's identity to be available.</param>
        public static void OnInitialized( Action action )
        {
            _token.Token.UnsafeRegister( _ => action(), null );
        }

        /// <summary>
        /// Gets the available identity.
        /// The first call to this property triggers the initialization of the identity
        /// and the calls to registered <see cref="OnInitialized(Action)"/> callbacks.
        /// </summary>
        public static CoreApplicationIdentity Instance
        {
            get
            {
                if( _instance == null )
                {
                    bool callInit = false;
                    // Simple double check locking.
                    lock( _token )
                    {
                        if( _instance == null )
                        {
                            Debug.Assert( _builder != null );
                            _instance = _builder.Build();
                            _builder = null;
                            callInit = true;
                        }
                    }
                    // Calls the callbacks outside the lock.
                    if( callInit ) _token.Cancel( throwOnFirstException: true );
                }
                return _instance;
            }
        }

    }
}
