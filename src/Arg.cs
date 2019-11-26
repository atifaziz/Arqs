#region Copyright (c) 2019 Atif Aziz. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

namespace Largs
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;

    partial interface IArg
    {
        string  Name        { get; }
        string  Description { get; }

        IAccumulator CreateAccumulator();
    }

    partial class ArgInfo
    {
        public ArgInfo() :
            this(null)  {}

        public ArgInfo(string name, string description = null)
        {
            Name          = name;
            Description   = description;
        }

        public string   Name        { get; }
        public string   Description { get; }

        public ArgInfo WithName(string value)
            => value == null ? throw new ArgumentNullException(nameof(value))
             : value == Name ? this
             : UpdateCore(value, Description);

        public ArgInfo WithDescription(string value) =>
            value == Description ? this : UpdateCore(Name, value);

        protected virtual ArgInfo UpdateCore(string name, string description) =>
            new ArgInfo(name, description);

        public override string ToString() => Name + String.ConcatAll(": " + Description);

        public Arg<T> ToArg<T>(IParser<T> parser, Func<IAccumulator> accumulatorFactory, Func<IAccumulator, T> binder) =>
            new Arg<T>(this, parser, accumulatorFactory, binder);
    }

    partial class Arg<T> : IArg, IArgBinder<T>
    {
        readonly ArgInfo _info;
        readonly Func<IAccumulator> _readerFactory;
        readonly Func<IAccumulator, T> _binder;

        public Arg(ArgInfo info, IParser<T> parser, Func<IAccumulator> accumulatorFactory, Func<IAccumulator, T> binder)
        {
            _info = info ?? throw new ArgumentNullException(nameof(info));
            _readerFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public string  Name        => _info.Name;
        public string  Description => _info.Description;

        public IParser<T> Parser { get; }

        public IAccumulator CreateAccumulator() => _readerFactory();

        Arg<T> WithInfo(ArgInfo value) =>
            value == _info ? this : new Arg<T>(value, Parser, _readerFactory, _binder);

        public Arg<T> WithName(string value) =>
            WithInfo(_info.WithName(value));

        public Arg<T> WithDescription(string value) =>
            WithInfo(_info.WithDescription(value));

        public Arg<(bool Present, T Value)> FlagPresence() =>
            FlagPresence(false, true);

        public Arg<(TPresence Presence, T Value)> FlagPresence<TPresence>(TPresence absent, TPresence present) =>
            new Arg<(TPresence, T)>(_info,
                                    from v in Parser select (present, v),
                                    _readerFactory, r => r.HasValue ? (present, _binder(r)) : (absent, default));

        public T Bind(Func<IArg, IAccumulator> source) =>
            _binder(source(this));

        public void Inspect(ICollection<IArg> args) => args.Add(this);
    }

    partial class ListArg<T> : IArg, IArgBinder<ImmutableArray<T>>
    {
        readonly Arg<T> _arg;

        public ListArg(Arg<T> arg)
        {
            _arg = arg ?? throw new ArgumentNullException(nameof(arg));
        }

        public string Name => _arg.Name;
        public string Description => _arg.Description;

        public IParser<T> ItemParser => _arg.Parser;

        public IAccumulator CreateAccumulator() => new Accumulator(_arg);

        ListArg<T> WithArg(Arg<T> value) =>
            value == _arg ? this : new ListArg<T>(value);

        public ListArg<T> WithName(string value) =>
            WithArg(_arg.WithName(value));

        public ListArg<T> WithDescription(string value) =>
            WithArg(_arg.WithDescription(value));

        public ImmutableArray<T> Bind(Func<IArg, IAccumulator> source) =>
            ((Accumulator)source(this)).Value.ToImmutable();

        public void Inspect(ICollection<IArg> args) => args.Add(this);

        sealed class Accumulator : IAccumulator<ImmutableArray<T>.Builder>
        {
            readonly Arg<T> _arg;

            public Accumulator(Arg<T> arg) => _arg = arg;

            public bool HasValue => true;

            public ImmutableArray<T>.Builder Value { get; } = ImmutableArray.CreateBuilder<T>();

            object IAccumulator.Value => Value;

            public bool Read(Reader<string> arg)
            {
                var reader = _arg.CreateAccumulator();
                if (!reader.Read(arg))
                    return false;
                Value.Add(_arg.Bind(_ => reader));
                return true;
            }
        }
    }

    partial class TailArg<T> : IArg, IArgBinder<ImmutableArray<T>>
    {
        readonly Arg<T> _arg;

        public TailArg(Arg<T> arg) =>
            _arg = arg ?? throw new ArgumentNullException(nameof(arg));

        public string Name => _arg.Name;
        public string Description => _arg.Description;

        public IParser<T> ItemParser => _arg.Parser;

        public IAccumulator CreateAccumulator() => new Accumulator(_arg);

        TailArg<T> WithArg(Arg<T> value) =>
            value == _arg ? this : new TailArg<T>(value);

        public TailArg<T> WithName(string value) =>
            WithArg(_arg.WithName(value));

        public TailArg<T> WithDescription(string value) =>
            WithArg(_arg.WithDescription(value));

        public ImmutableArray<T> Bind(Func<IArg, IAccumulator> source) =>
            ((Accumulator)source(this)).Value.ToImmutable();

        public void Inspect(ICollection<IArg> args) => args.Add(this);

        sealed class Accumulator : IAccumulator<ImmutableArray<T>.Builder>
        {
            readonly Arg<T> _arg;

            public Accumulator(Arg<T> arg) => _arg = arg;

            public bool HasValue => true;

            public ImmutableArray<T>.Builder Value { get; } = ImmutableArray.CreateBuilder<T>();

            object IAccumulator.Value => Value;

            public bool Read(Reader<string> arg)
            {
                while (arg.HasMore())
                {
                    var reader = _arg.CreateAccumulator();
                    if (!reader.Read(arg))
                        return false;
                    Value.Add(_arg.Bind(_ => reader));
                }
                return true;
            }
        }
    }

    static partial class Args
    {
        public static Arg<bool> Flag(string name) =>
            new ArgInfo(name).ToArg(Parser.Create<bool>(_ => throw new NotSupportedException()), Accumulator.Flag, r => r.HasValue);

        public static Arg<T> Option<T>(string name, T @default, IParser<T> parser) =>
            new ArgInfo(name).ToArg(parser, () => Accumulator.Value(parser), r => r.HasValue ? ((IAccumulator<T>)r).Value : @default);

        public static Arg<T> Option<T>(string name, IParser<T> parser) =>
            Option(name, default, parser);

        public static Arg<T> Arg<T>(string name, T @default, IParser<T> parser) =>
            new ArgInfo().ToArg(parser, () => Accumulator.Value(parser), r => r.HasValue ? ((IAccumulator<T>)r).Value : @default);

        public static Arg<T> Arg<T>(string name, IParser<T> parser) =>
            Arg(name, default, parser);

        public static TailArg<T> Tail<T>(this Arg<T> arg) =>
            new TailArg<T>(arg);

        public static ListArg<T> List<T>(this Arg<T> arg) =>
            new ListArg<T>(arg);
    }

    partial interface IAccumulator
    {
        bool HasValue { get; }
        object Value { get; }
        bool Read(Reader<string> arg);
    }

    partial interface IAccumulator<out T> : IAccumulator
    {
        new T Value { get; }
    }

    partial interface IArgBinder<out T>
    {
        T Bind(Func<IArg, IAccumulator> source);
        void Inspect(ICollection<IArg> args);
    }

    static partial class Accumulator
    {
        public static IAccumulator<T> Value<T>(IParser<T> parser) =>
            new ValueAccumulator<T>(default, (_, arg) => arg.TryRead(out var v) ? parser.Parse(v) : default);

        public static IAccumulator<int> Flag() =>
            new ValueAccumulator<int>(0, (count, _) => ParseResult.Success(count + 1));

        sealed class ValueAccumulator<T> : IAccumulator<T>
        {
            public bool HasValue { get; private set; }
            public T Value { get; private set; }

            object IAccumulator.Value => Value;

            readonly Func<T, Reader<string>, ParseResult<T>> _reader;

            public ValueAccumulator(T initial, Func<T, Reader<string>, ParseResult<T>> reader)
            {
                Value = initial;
                _reader = reader ?? throw new ArgumentNullException(nameof(reader));
            }

            public bool Read(Reader<string> arg)
            {
                switch (_reader(Value, arg))
                {
                    case (true, var value):
                        HasValue = true;
                        Value = value;
                        return true;
                    default:
                        Value = default;
                        return false;
                }
            }
        }
    }

    static partial class ArgBinder
    {
        public static IList<IArg> Inspect<T>(this IArgBinder<T> binder)
        {
            var infos = new List<IArg>();
            binder.Inspect(infos);
            return infos;
        }

        public static IArgBinder<(T, U)> Zip<T, U>(this IArgBinder<T> first, IArgBinder<U> second) =>
            Create(bindings => (first.Bind(bindings), second.Bind(bindings)),
                   args     => { first.Inspect(args); second.Inspect(args); });

        public static (T Result, ImmutableArray<string> Tail)
            Bind<T>(this IArgBinder<T> binder, params string[] args)
        {
            var infos = new List<IArg>();
            binder.Inspect(infos);
            var asi = 0;
            var values = new IAccumulator[infos.Count];
            for (var i = 0; i < infos.Count; i++)
                values[i] = infos[i].CreateAccumulator();
            using var e = new Reader<string>(args);
            var tail = new List<string>();
            while (e.TryPeek(out var arg))
            {
                if (arg.StartsWith("--", StringComparison.Ordinal))
                {
                    var name = arg.Substring(2);
                    var i = infos.FindIndex(e => e.Name == name);
                    if (i < 0)
                    {
                        i = infos.FindIndex(asi, e => e.Name == null);
                        if (i < 0)
                        {
                            e.Read();
                            tail.Add(arg);
                        }
                        else
                        {
                            asi = i + 1;
                            if (!values[i].Read(e))
                                throw new Exception("Invalid argument: " + arg);
                        }
                    }
                    else
                    {
                        e.Read();
                        if (!values[i].Read(e))
                            throw new Exception("Invalid value for option: " + name);
                    }
                }
                else if (arg.Length > 1 && arg[0] == '-')
                {
                    if (arg.Length > 2)
                    {
                        e.Read();
                        foreach (var ch in arg.Substring(1).Reverse())
                        {
                            var i = infos.FindIndex(e => e.Name.Length == 1 && e.Name[0] == ch);
                            if (i < 0)
                                throw new Exception("Invalid option: " + ch);
                            e.Unread("-" + ch);
                        }
                    }
                    else
                    {
                        var ch = arg[1];
                        var i = infos.FindIndex(e => e.Name.Length == 1 && e.Name[0] == ch);
                        if (i < 0)
                            throw new Exception("Invalid option: " + ch);
                        e.Read();
                        if (!values[i].Read(e))
                            throw new Exception("Invalid value for option: " + ch);
                    }
                }
                else
                {
                    var i = infos.FindIndex(asi, e => e.Name == null);
                    if (i < 0)
                    {
                        e.Read();
                        tail.Add(arg);
                    }
                    else
                    {
                        asi = i + 1;
                        if (!values[i].Read(e))
                            throw new Exception("Invalid argument: " + arg);
                    }
                }
            }

            return (binder.Bind(info => values[infos.IndexOf(info)]), tail.ToImmutableArray());
        }

        public static IArgBinder<T> Create<T>(Func<Func<IArg, IAccumulator>, T> binder, Action<ICollection<IArg>> inspector) =>
            new DelegatingArgBinder<T>(binder, inspector);

        public static IArgBinder<U> Select<T, U>(this IArgBinder<T> binder, Func<T, U> f) =>
            Create(bindings => f(binder.Bind(bindings)), binder.Inspect);

        public static IArgBinder<U> SelectMany<T, U>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f) =>
            Create(bindings => f(binder.Bind(bindings)).Bind(bindings),
                   args =>
                   {
                       binder.Inspect(args);
                       f(binder.Bind(delegate { throw new InvalidOperationException(); })).Inspect(args);
                   });

        public static IArgBinder<V> SelectMany<T, U, V>(this IArgBinder<T> binder, Func<T, IArgBinder<U>> f, Func<T, U, V> g) =>
            binder.Select(t => f(t).Select(u => g(t, u))).SelectMany(pv => pv);

        public static IArgBinder<V> Join<T, U, K, V>(this IArgBinder<T> first, IArgBinder<U> second,
                                                     Func<T, K> unused1, Func<T, K> unused2,
                                                     Func<T, U, V> resultSelector) =>
            from ab in first.Zip(second)
            select resultSelector(ab.Item1, ab.Item2);

        sealed class DelegatingArgBinder<T> : IArgBinder<T>
        {
            readonly Func<Func<IArg, IAccumulator>, T> _binder;
            readonly Action<ICollection<IArg>> _inspector;

            public DelegatingArgBinder(Func<Func<IArg, IAccumulator>, T> binder,
                                       Action<ICollection<IArg>> inspector)
            {
                _binder = binder;
                _inspector = inspector;
            }

            public T Bind(Func<IArg, IAccumulator> source) =>
                _binder(source);

            public void Inspect(ICollection<IArg> args) =>
                _inspector(args);
        }
    }

    sealed partial class Reader<T> : IDisposable
    {
        (bool, T) _next;
        Stack<T> _nextItems;
        IEnumerator<T> _enumerator;

        public Reader(IEnumerable<T> items) =>
            _enumerator = items.GetEnumerator();

        public void Dispose()
        {
            var args  = _enumerator;
            _enumerator = null;
            args?.Dispose();
        }

        public bool HasMore() => TryPeek(out _);

        public bool TryPeek(out T item)
        {
            if (!TryRead(out item))
                return false;
            Unread(item);
            return true;
        }

        internal void Unread(T item)
        {
            var (hasNext, next) = _next;
            if (hasNext)
            {
                _next = default;
                _nextItems = new Stack<T>();
                _nextItems.Push(next);
            }
            else if (_nextItems == null)
            {
                _next = (true, item);
                return;
            }

            _nextItems.Push(item);
        }

        public T Read() =>
            TryRead(out var item) ? item : throw new InvalidOperationException();

        public bool TryRead(out T item)
        {
            var (hasNext, next) = _next;

            if (hasNext)
            {
                item = next;
                _next = default;
                return true;
            }

            if (_nextItems?.Count > 0)
            {
                item = _nextItems.Pop();
                return true;
            }

            if (_enumerator == null)
            {
                item = default;
                return false;
            }

            if (!_enumerator.MoveNext())
            {
                _enumerator.Dispose();
                _enumerator = null;
                item = default;
                return false;
            }

            item = _enumerator.Current;
            return true;
        }
    }
}
