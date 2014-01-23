#region LGPL License
/*----------------------------------------------------------------------------
* This file (CK.Core\ActivityMonitor\Impl\ActivityMonitorExtension.cs) is part of CiviKey. 
*  
* CiviKey is free software: you can redistribute it and/or modify 
* it under the terms of the GNU Lesser General Public License as published 
* by the Free Software Foundation, either version 3 of the License, or 
* (at your option) any later version. 
*  
* CiviKey is distributed in the hope that it will be useful, 
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the 
* GNU Lesser General Public License for more details. 
* You should have received a copy of the GNU Lesser General Public License 
* along with CiviKey.  If not, see <http://www.gnu.org/licenses/>. 
*  
* Copyright © 2007-2012, 
*     Invenietis <http://www.invenietis.com>,
*     In’Tech INFO <http://www.intechinfo.fr>,
* All rights reserved. 
*-----------------------------------------------------------------------------*/
#endregion

using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;

namespace CK.Core
{
    /// <summary>
    /// Provides extension methods for <see cref="IActivityMonitor"/> and other types from the Activity monitor framework.
    /// </summary>
    public static partial class ActivityMonitorExtension
    {

        /// <summary>
        /// Returns a valid <see cref="DateTimeStamp"/> that will be used for a log: it is based on <see cref="DateTime.UtcNow"/> and has 
        /// a <see cref="DateTimeStamp.Uniquifier"/> that will not be changed when emitting the next log.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <returns>The next log time for the monitor.</returns>
        public static DateTimeStamp NextLogTime( this IActivityMonitor @this )
        {
            return new DateTimeStamp( @this.LastLogTime, DateTime.UtcNow );
        }

        /// <summary>
        /// Challenges FileName/LineNumber filters, <see cref="IActivityMonitor.ActualFilter">this monitors' filter</see> and application 
        /// domain's <see cref="ActivityMonitor.DefaultFilter"/> filters to test whether a log line should actually be emitted.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">Log level.</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        /// <returns>True if the log should be emitted.</returns>
        public static bool ShouldLogLine( this IActivityMonitor @this, LogLevel level, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            level &= LogLevel.Mask;
            int filter;
            if( (filter = ActivityMonitor.SourceFilter.SourceFilterLine( ref fileName, lineNumber )) == 0 ) filter = (int)@this.ActualFilter.Line;
            return filter <= 0 ? (int)ActivityMonitor.DefaultFilter.Line <= (int)level : filter <= (int)level;
        }

        /// <summary>
        /// Challenges FileName/LineNumber filters, <see cref="IActivityMonitor.ActualFilter">this monitors'filter</see> and application 
        /// domain's <see cref="ActivityMonitor.DefaultFilter"/> filters to test whether a log group should actually be emitted.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="level">Log level.</param>
        /// <param name="fileName">Source file name of the emitter (automatically injected by C# compiler but can be explicitly set).</param>
        /// <param name="lineNumber">Line number in the source file (automatically injected by C# compiler but can be explicitly set).</param>
        /// <returns>True if the log should be emitted.</returns>
        public static bool ShouldLogGroup( this IActivityMonitor @this, LogLevel level, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            level &= LogLevel.Mask;
            int filter;
            if( (filter = ActivityMonitor.SourceFilter.SourceFilterGroup( ref fileName, lineNumber )) == 0 ) filter = (int)@this.ActualFilter.Group;
            return filter <= 0 ? (int)ActivityMonitor.DefaultFilter.Group <= (int)level : filter <= (int)level;
        }

        /// <summary>
        /// Private method used by XXX (Trace, Info,..., Fatal) extension methods.
        /// </summary>
        static IActivityMonitorLineSender FilterLogLine( this IActivityMonitor @this, LogLevel level, string fileName, int lineNumber )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            int filter;
            if( (filter = ActivityMonitor.SourceFilter.SourceFilterLine( ref fileName, lineNumber )) == 0 ) filter = (int)@this.ActualFilter.Line;
            if( filter <= 0 ? (int)ActivityMonitor.DefaultFilter.Line <= (int)level : filter <= (int)level )
            {
                return new ActivityMonitorLineSender( @this, level | LogLevel.IsFiltered, fileName, lineNumber );
            }
            return ActivityMonitorLineSender.FakeLineSender;
        }

        /// <summary>
        /// Private method used by OpenXXX (Trace, Info,..., Fatal) extension methods.
        /// </summary>
        static IActivityMonitorGroupSender FilteredGroup( IActivityMonitor @this, LogLevel level, string fileName, int lineNumber )
        {
            Debug.Assert( (level & LogLevel.IsFiltered) == 0 );
            int filter;
            if( (filter = ActivityMonitor.SourceFilter.SourceFilterGroup( ref fileName, lineNumber )) == 0 ) filter = (int)@this.ActualFilter.Group;
            if( filter <= 0 ? (int)ActivityMonitor.DefaultFilter.Group <= (int)level : filter <= (int)level )
            {
                return new ActivityMonitorGroupSender( @this, level | LogLevel.IsFiltered, fileName, lineNumber );
            }
            return new ActivityMonitorGroupSender( @this );
        }

        /// <summary>
        /// Logs a text regardless of <see cref="IActivityMonitor.ActualFilter">ActualFilter</see> level. 
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="tags">
        /// Tags (from <see cref="ActivityMonitor.Tags"/>) to associate to the log. 
        /// These tags will be union-ed with the current <see cref="IActivityMonitor.AutoTags">AutoTags</see>.
        /// </param>
        /// <param name="level">Log level. Must not be <see cref="LogLevel.None"/>.</param>
        /// <param name="text">Text to log. Must not be null or empty.</param>
        /// <param name="logTime">
        /// Time-stamp of the log entry.
        /// You can use <see cref="LogTime.UtcNow"/> or <see cref="ActivityMonitorExtension.NextLogTime">IActivityMonitor.NextLogTime()</see> extension method.</param>
        /// </param>
        /// <param name="ex">Optional exception associated to the log. When not null, a Group is automatically created.</param>
        /// <param name="fileName">The source code file name from which the log is emitted.</param>
        /// <param name="lineNumber">The line number in the source from which the log is emitted.</param>
        /// <remarks>
        /// The <paramref name="text"/> can not be null or empty.
        /// <para>
        /// Each call to log is considered as a unit of text: depending on the rendering engine, a line or a 
        /// paragraph separator (or any appropriate separator) should be appended between each text if 
        /// the <paramref name="level"/> is the same as the previous one.
        /// </para>
        /// <para>If needed, the special text <see cref="ActivityMonitor.ParkLevel"/> ("PARK-LEVEL") can be used as a convention 
        /// to break the current <see cref="LogLevel"/> and resets it: the next log, even with the same LogLevel, should be 
        /// treated as if a different LogLevel is used.
        /// </para>
        /// </remarks>
        static public void UnfilteredLog( this IActivityMonitor @this, CKTrait tags, LogLevel level, string text, DateTimeStamp logTime, Exception ex, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            @this.UnfilteredLog( new ActivityMonitorLogData( level, ex, tags, text, logTime, fileName, lineNumber ) );
        }

        /// <summary>
        /// Opens a group regardless of <see cref="IActivityMonitor.ActualFilter">ActualFilter</see> level. 
        /// <see cref="CloseGroup"/> must be called in order to close the group, and/or the returned object must be disposed (both safely can be called: 
        /// the group is closed on the first action, the second one is ignored).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="tags">Tags (from <see cref="ActivityMonitor.Tags"/>) to associate to the log. It will be unioned with current <see cref="IActivityMonitor.AutoTags">AutoTags</see>.</param>
        /// <param name="level">Log level. The <see cref="LogLevel.None"/> level is used to open a filtered group. See remarks.</param>
        /// <param name="getConclusionText">Optional function that will be called on group closing.</param>
        /// <param name="text">Text to log (the title of the group). Null text is valid and considered as <see cref="String.Empty"/> or assigned to the <see cref="Exception.Message"/> if it exists.</param>
        /// <param name="logTime">
        /// Time of the log entry.
        /// You can use <see cref="LogTime.UtcNow"/> or <see cref="ActivityMonitorExtension.NextLogTime">IActivityMonitor.NextLogTime()</see> extension method.</param>
        /// </param>
        /// <param name="ex">Optional exception associated to the group.</param>
        /// <param name="fileName">The source code file name from which the group is opened.</param>
        /// <param name="lineNumber">The line number in the source from which the group is opened.</param>
        /// <returns>A disposable object that can be used to close the group.</returns>
        /// <remarks>
        /// <para>
        /// Opening a group does not change the current <see cref="IActivityMonitor.MinimalFilter">MinimalFilter</see>, except when 
        /// opening a <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/> group: in such case, the Filter is automatically 
        /// sets to <see cref="LogFilter.Debug"/> to capture all potential information inside the error group.
        /// </para>
        /// <para>
        /// Changes to the monitor's current Filter or AutoTags that occur inside a group are automatically restored to their original values when the group is closed.
        /// This behavior guaranties that a local modification (deep inside unknown called code) does not impact caller code: groups are a way to easily isolate such 
        /// configuration changes.
        /// </para>
        /// <para>
        /// Note that this automatic configuration restoration works even if the group is filtered (when the <paramref name="level"/> is None).
        /// </para>
        /// </remarks>
        static public IDisposable UnfilteredOpenGroup( this IActivityMonitor @this, CKTrait tags, LogLevel level, Func<string> getConclusionText, string text, DateTimeStamp logTime, Exception ex, [CallerFilePath]string fileName = null, [CallerLineNumber]int lineNumber = 0 )
        {
            return @this.UnfilteredOpenGroup( new ActivityMonitorGroupData( level, tags, text, logTime, ex, getConclusionText, fileName, lineNumber ) );
        }

        /// <summary>
        /// Closes the current Group. Optional parameter is polymorphic. It can be a string, a <see cref="ActivityLogGroupConclusion"/>, 
        /// a <see cref="List{T}"/> or an <see cref="IEnumerable{T}"/> of ActivityLogGroupConclusion, or any object with an overridden <see cref="Object.ToString"/> method. 
        /// See remarks (especially for List&lt;ActivityLogGroupConclusion&gt;).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="userConclusion">Optional string, ActivityLogGroupConclusion object, enumerable of ActivityLogGroupConclusion or object to conclude the group. See remarks.</param>
        /// <remarks>
        /// An untyped object is used here to easily and efficiently accommodate both string and already existing ActivityLogGroupConclusion.
        /// When a List&lt;ActivityLogGroupConclusion&gt; is used, it will be directly used to collect conclusion objects (new conclusions will be added to it). This is an optimization.
        /// </remarks>
        public static void CloseGroup( this IActivityMonitor @this, object userConclusion = null )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            @this.CloseGroup( NextLogTime( @this ), userConclusion );
        }
        
        #region Bridge: FindBridgeTo, CreateBridgeTo and UnbridgeTo.

        /// <summary>
        /// Finds an existing bridge to another monitor.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/>.</param>
        /// <param name="targetBridge">The target bridge that receives our logs.</param>
        /// <returns>The existing <see cref="ActivityMonitorBridge"/> or null if no such bridge exists.</returns>
        public static ActivityMonitorBridge FindBridgeTo( this IActivityMonitorOutput @this, ActivityMonitorBridgeTarget targetBridge )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( targetBridge == null ) throw new ArgumentNullException( "targetBridge" );
            return @this.Clients.OfType<ActivityMonitorBridge>().FirstOrDefault( b => b.BridgeTarget == targetBridge );
        }

        /// <summary>
        /// Creates a bridge to another monitor's <see cref="ActivityMonitorBridgeTarget"/>. Only one bridge to the same monitor can exist at a time: if <see cref="FindBridgeTo"/> is not null, 
        /// this throws a <see cref="InvalidOperationException"/>.
        /// This bridge does not synchronize <see cref="IActivityMonitor.AutoTags"/> and <see cref="IActivityMonitor.Topic"/> (see <see cref="CreateStrongBridgeTo"/>). 
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/>.</param>
        /// <param name="targetBridge">The target bridge that will receive our logs.</param>
        /// <returns>A <see cref="IDisposable"/> object that can be disposed to automatically call <see cref="UnbridgeTo"/>.</returns>
        public static IDisposable CreateBridgeTo( this IActivityMonitorOutput @this, ActivityMonitorBridgeTarget targetBridge )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( targetBridge == null ) throw new ArgumentNullException( "targetBridge" );
            if( @this.Clients.OfType<ActivityMonitorBridge>().Any( b => b.BridgeTarget == targetBridge ) ) throw new InvalidOperationException();
            var created = @this.RegisterClient( new ActivityMonitorBridge( targetBridge, false, false ) );
            return Util.CreateDisposableAction( () => @this.UnregisterClient( created ) );
        }

        /// <summary>
        /// Creates a strong bridge to another monitor's <see cref="ActivityMonitorBridgeTarget"/>. 
        /// Only one bridge to the same monitor can exist at a time: if <see cref="FindBridgeTo"/> is not null, 
        /// this throws a <see cref="InvalidOperationException"/>.
        /// A strong bridge synchronizes <see cref="IActivityMonitor.AutoTags"/> and <see cref="IActivityMonitor.Topic"/> between the two monitors. When created, the 2 properties
        /// of the local monitor are set to the ones of the target monitor. 
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/>.</param>
        /// <param name="targetBridge">The target bridge that will receive our logs.</param>
        /// <returns>A <see cref="IDisposable"/> object that can be disposed to automatically call <see cref="UnbridgeTo"/>.</returns>
        public static IDisposable CreateStrongBridgeTo( this IActivityMonitorOutput @this, ActivityMonitorBridgeTarget targetBridge )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( targetBridge == null ) throw new ArgumentNullException( "targetBridge" );
            if( @this.Clients.OfType<ActivityMonitorBridge>().Any( b => b.BridgeTarget == targetBridge ) ) throw new InvalidOperationException();
            var created = @this.RegisterClient( new ActivityMonitorBridge( targetBridge, true, true ) );
            return Util.CreateDisposableAction( () => @this.UnregisterClient( created ) );
        }

        /// <summary>
        /// Removes an existing <see cref="ActivityMonitorBridge"/> to another monitor if it exists (silently ignores it if not found).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/>.</param>
        /// <param name="targetBridge">The target bridge that will no more receive our logs.</param>
        /// <returns>The unregistered <see cref="ActivityMonitorBridge"/> if found, null otherwise.</returns>
        public static ActivityMonitorBridge UnbridgeTo( this IActivityMonitorOutput @this, ActivityMonitorBridgeTarget targetBridge )
        {
            if( targetBridge == null ) throw new ArgumentNullException( "targetBridge" );
            return UnregisterClient<ActivityMonitorBridge>( @this, b => b.BridgeTarget == targetBridge );
        }

        #endregion


        #region Catch & CatchCounter

        /// <summary>
        /// Enables simple "using" syntax to easily catch any <see cref="LogLevel"/> (or above) entries (defaults to <see cref="LogLevel.Error"/>).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="errorHandler">An action that accepts a list of fatal or error <see cref="ActivityMonitorSimpleCollector.Entry">entries</see>.</param>
        /// <param name="level">Defines the level of the entries caught (by default fatal or error entries).</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable Catch( this IActivityMonitor @this, Action<IReadOnlyList<ActivityMonitorSimpleCollector.Entry>> errorHandler, LogLevelFilter level = LogLevelFilter.Error )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( errorHandler == null ) throw new ArgumentNullException( "errorHandler" );
            ActivityMonitorSimpleCollector errorTracker = new ActivityMonitorSimpleCollector() { MinimalFilter = level };
            @this.Output.RegisterClient( errorTracker );
            return Util.CreateDisposableAction( () =>
            {
                @this.Output.UnregisterClient( errorTracker );
                if( errorTracker.Entries.Count > 0 ) errorHandler( errorTracker.Entries );
            } );
        }

        /// <summary>
        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/>, <see cref="LogLevel.Error"/> or <see cref="LogLevel.Warn"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="fatalErrorWarnCount">An action that accepts three counts for fatals, errors and warnings.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable CatchCounter( this IActivityMonitor @this, Action<int, int, int> fatalErrorWarnCount )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( fatalErrorWarnCount == null ) throw new ArgumentNullException( "fatalErrorWarnCount" );
            ActivityMonitorErrorCounter errorCounter = new ActivityMonitorErrorCounter();
            Debug.Assert( errorCounter.GenerateConclusion == false, "It is false by default." );
            @this.Output.RegisterClient( errorCounter );
            return Util.CreateDisposableAction( () =>
            {
                @this.Output.UnregisterClient( errorCounter );
                if( errorCounter.Current.HasWarnOrError ) fatalErrorWarnCount( errorCounter.Current.FatalCount, errorCounter.Current.ErrorCount, errorCounter.Current.WarnCount );
            } );
        }

        /// <summary>
        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/> and <see cref="LogLevel.Error"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="fatalErrorCount">An action that accepts two counts for fatals and errors.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable CatchCounter( this IActivityMonitor @this, Action<int, int> fatalErrorCount )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( fatalErrorCount == null ) throw new ArgumentNullException( "fatalErrorCount" );
            ActivityMonitorErrorCounter errorCounter = new ActivityMonitorErrorCounter() { GenerateConclusion = false };
            @this.Output.RegisterClient( errorCounter );
            return Util.CreateDisposableAction( () =>
            {
                @this.Output.UnregisterClient( errorCounter );
                if( errorCounter.Current.HasError ) fatalErrorCount( errorCounter.Current.FatalCount, errorCounter.Current.ErrorCount );
            } );
        }

        /// <summary>
        /// Enables simple "using" syntax to easily detect <see cref="LogLevel.Fatal"/> or <see cref="LogLevel.Error"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/>.</param>
        /// <param name="fatalOrErrorCount">An action that accepts one count that sums fatals and errors.</param>
        /// <returns>A <see cref="IDisposable"/> object used to manage the scope of this handler.</returns>
        public static IDisposable CatchCounter( this IActivityMonitor @this, Action<int> fatalOrErrorCount )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( fatalOrErrorCount == null ) throw new ArgumentNullException( "fatalErrorCount" );
            ActivityMonitorErrorCounter errorCounter = new ActivityMonitorErrorCounter() { GenerateConclusion = false };
            @this.Output.RegisterClient( errorCounter );
            return Util.CreateDisposableAction( () =>
            {
                @this.Output.UnregisterClient( errorCounter );
                if( errorCounter.Current.HasError ) fatalOrErrorCount( errorCounter.Current.FatalCount + errorCounter.Current.ErrorCount );
            } );
        }
        
        #endregion


        #region IActivityMonitor.SetFilter( level )

        class LogFilterSentinel : IDisposable
        {
            IActivityMonitor _monitor;
            LogFilter _prevLevel;

            public LogFilterSentinel( IActivityMonitor l, LogFilter filter )
            {
                _prevLevel = l.MinimalFilter;
                _monitor = l;
                l.MinimalFilter = filter;
            }

            public void Dispose()
            {
                _monitor.MinimalFilter = _prevLevel;
            }

        }

        /// <summary>
        /// Sets filter levels on this <see cref="IActivityMonitor"/>. The current <see cref="IActivityMonitor.MinimalFilter"/> will be automatically 
        /// restored when the returned <see cref="IDisposable"/> will be disposed.
        /// Even if when a Group is closed, the IActivityMonitor.Filter is automatically restored to its original value 
        /// (captured when the Group was opened), this may be useful to locally change the filter level without bothering to restore the 
        /// initial value (this is what OpenGroup/CloseGroup do with both the Filter and the AutoTags).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/> object.</param>
        /// <param name="filterLineLevel">The new filter level for log line.</param>
        /// <param name="filterGroupLevel">The new filter level for group.</param>
        /// <returns>A <see cref="IDisposable"/> object that will restore the current level.</returns>
        public static IDisposable SetFilter( this IActivityMonitor @this, LogLevelFilter filterLineLevel, LogLevelFilter filterGroupLevel )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            return new LogFilterSentinel( @this, new LogFilter( filterGroupLevel, filterLineLevel ) );
        }

        /// <summary>
        /// Sets a filter level on this <see cref="IActivityMonitor"/>. The current <see cref="IActivityMonitor.MinimalFilter"/> will be automatically 
        /// restored when the returned <see cref="IDisposable"/> will be disposed.
        /// Even if when a Group is closed, the IActivityMonitor.Filter is automatically restored to its original value 
        /// (captured when the Group was opened), this may be useful to locally change the filter level without bothering to restore the 
        /// initial value (this is what OpenGroup/CloseGroup do with both the Filter and the AutoTags).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/> object.</param>
        /// <param name="f">The new filter.</param>
        /// <returns>A <see cref="IDisposable"/> object that will restore the current level.</returns>
        public static IDisposable SetFilter( this IActivityMonitor @this, LogFilter f )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            return new LogFilterSentinel( @this, f );
        }

        #endregion IActivityMonitor.SetFilter( level )


        #region IActivityMonitor.SetAutoTags( Tags, SetOperation )

        class TagsSentinel : IDisposable
        {
            readonly IActivityMonitor _monitor;
            readonly CKTrait _previous;

            public TagsSentinel( IActivityMonitor l, CKTrait t )
            {
                _previous = l.AutoTags;
                _monitor = l;
                l.AutoTags = t;
            }

            public void Dispose()
            {
                _monitor.AutoTags = _previous;
            }

        }

        /// <summary>
        /// Alter tags of this <see cref="IActivityMonitor"/>. Current <see cref="IActivityMonitor.AutoTags"/> will be automatically 
        /// restored when the returned <see cref="IDisposable"/> will be disposed.
        /// Even if when a Group is closed, the IActivityMonitor.AutoTags is automatically restored to its original value 
        /// (captured when the Group was opened), this may be useful to locally change the tags level without bothering to restore the 
        /// initial value (this is close to what OpenGroup/CloseGroup do with both the Filter and the AutoTags).
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitor"/> object.</param>
        /// <param name="tags">Tags to combine with the current one.</param>
        /// <param name="operation">Defines the way the new <paramref name="tags"/> must be combined with current ones.</param>
        /// <returns>A <see cref="IDisposable"/> object that will restore the current tag when disposed.</returns>
        public static IDisposable SetAutoTags( this IActivityMonitor @this, CKTrait tags, SetOperation operation = SetOperation.Union )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            return new TagsSentinel( @this, @this.AutoTags.Apply( tags, operation ) );
        }
        
        #endregion


        #region RegisterClients

        /// <summary>
        /// Registers multiple <see cref="IActivityMonitorClient"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/> object.</param>
        /// <param name="clients">Multiple clients to register.</param>
        /// <returns>This registrar to enable fluent syntax.</returns>
        public static IActivityMonitorOutput RegisterClients( this IActivityMonitorOutput @this, IEnumerable<IActivityMonitorClient> clients )
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            foreach( var c in clients ) @this.RegisterClient( c );
            return @this;
        }

        /// <summary>
        /// Registers multiple <see cref="IActivityMonitorClient"/>.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/> object.</param>
        /// <param name="clients">Multiple clients to register.</param>
        /// <returns>This registrar to enable fluent syntax.</returns>
        public static IActivityMonitorOutput RegisterClients( this IActivityMonitorOutput @this, params IActivityMonitorClient[] clients )
        {
            return RegisterClients( @this, (IEnumerable<IActivityMonitorClient>)clients );
        }

        /// <summary>
        /// Registers a unique client for a type that must have a public default constructor. 
        /// <see cref="Activator.CreateInstance{T}()"/> is called if necessary.
        /// </summary>
        /// <returns>The found or newly created client.</returns>
        public static T RegisterUniqueClient<T>( this IActivityMonitorOutput @this ) 
            where T : IActivityMonitorClient, new()
        {
            return @this.RegisterUniqueClient( c => true, () => Activator.CreateInstance<T>() );
        }

        /// <summary>
        /// Unregisters the first <see cref="IActivityMonitorClient"/> from the <see cref="IActivityMonitorOutput.Clients"/> list
        /// that satisfies the predicate.
        /// </summary>
        /// <param name="this">This <see cref="IActivityMonitorOutput"/>.</param>
        /// <param name="predicate">A predicate that will be used to determine the first client to unregister.</param>
        /// <returns>The unregistered client, or null if no client has been found.</returns>
        public static T UnregisterClient<T>( this IActivityMonitorOutput @this, Func<T, bool> predicate ) where T : IActivityMonitorClient
        {
            if( @this == null ) throw new NullReferenceException( "this" );
            if( predicate == null ) throw new ArgumentNullException( "predicate" );
            T c = @this.Clients.OfType<T>().Where( predicate ).FirstOrDefault();
            if( c != null ) @this.UnregisterClient( c );
            return c;
        }

        #endregion

        /// <summary>
        /// Gets this Group conclusions as a readable string.
        /// </summary>
        /// <param name="this">This group conclusion. Can be null.</param>
        /// <param name="conclusionSeparator">Conclusion separator.</param>
        /// <returns>A lovely concatenated string of conclusions.</returns>
        public static string ToStringGroupConclusion( this IEnumerable<ActivityLogGroupConclusion> @this, string conclusionSeparator = " - " )
        {
            if( @this == null ) return String.Empty;
            StringBuilder b = new StringBuilder();
            foreach( var e in @this )
            {
                if( b.Length > 0 ) b.Append( conclusionSeparator );
                b.Append( e.Text );
            }
            return b.ToString();
        }

        /// <summary>
        /// Gets the path as a readable string.
        /// </summary>
        /// <param name="this">This path. Can be null.</param>
        /// <param name="elementSeparator">Between elements.</param>
        /// <param name="withoutConclusionFormat">There must be 3 placeholders {0} for the level, {1} for the text and {2} for the conclusion.</param>
        /// <param name="withConclusionFormat">There must be 2 placeholders {0} for the level and {1} for the text.</param>
        /// <param name="conclusionSeparator">Conclusion separator.</param>
        /// <param name="fatal">For Fatal errors.</param>
        /// <param name="error">For Errors.</param>
        /// <param name="warn">For Warnings.</param>
        /// <param name="info">For Infos.</param>
        /// <param name="trace">For Traces.</param>
        /// <returns>A lovely path.</returns>
        public static string ToStringPath( this IEnumerable<ActivityMonitorPathCatcher.PathElement> @this,
            string elementSeparator = "> ",
            string withoutConclusionFormat = "{0}{1} ",
            string withConclusionFormat = "{0}{1} -{{ {2} }}",
            string conclusionSeparator = " - ",
            string fatal = "[Fatal]- ",
            string error = "[Error]- ",
            string warn = "[Warning]- ",
            string info = "[Info]- ",
            string trace = "" )
        {
            if( @this == null ) return String.Empty;
            StringBuilder b = new StringBuilder();
            foreach( var e in @this )
            {
                if( b.Length > 0 ) b.Append( elementSeparator );
                string prefix = trace;
                switch( e.MaskedLevel )
                {
                    case LogLevel.Fatal: prefix = fatal; break;
                    case LogLevel.Error: prefix = error; break;
                    case LogLevel.Warn: prefix = warn; break;
                    case LogLevel.Info: prefix = info; break;
                }
                if( e.GroupConclusion != null ) b.AppendFormat( withConclusionFormat, prefix, e.Text, e.GroupConclusion.ToStringGroupConclusion( conclusionSeparator ) );
                else b.AppendFormat( withoutConclusionFormat, prefix, e.Text );
            }
            return b.ToString();
        }

    }
}
