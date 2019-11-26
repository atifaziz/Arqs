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
}
