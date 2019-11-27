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

    public interface IArg
    {
        PropertySet Properties { get; }
        IArg WithProperties(PropertySet value);
        IAccumulator CreateAccumulator();
    }

    public class Arg<T> : IArg, IArgBinder<T>
    {
        readonly Func<IAccumulator> _readerFactory;
        readonly Func<IAccumulator, T> _binder;

        public Arg(IParser<T> parser, Func<IAccumulator> accumulatorFactory, Func<IAccumulator, T> binder) :
            this(PropertySet.Empty, parser, accumulatorFactory, binder) {}

        public Arg(PropertySet properties, IParser<T> parser, Func<IAccumulator> accumulatorFactory, Func<IAccumulator, T> binder)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _readerFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public PropertySet Properties { get; }

        IArg IArg.WithProperties(PropertySet value) => WithProperties(value);

        public Arg<T> WithProperties(PropertySet value) =>
            Properties == value ? this : new Arg<T>(value, Parser, _readerFactory, _binder);

        public IParser<T> Parser { get; }

        public IAccumulator CreateAccumulator() => _readerFactory();

        public Arg<(bool Present, T Value)> FlagPresence() =>
            FlagPresence(false, true);

        public Arg<(TPresence Presence, T Value)> FlagPresence<TPresence>(TPresence absent, TPresence present) =>
            new Arg<(TPresence, T)>(Properties,
                                    from v in Parser select (present, v),
                                    _readerFactory, r => r.HasValue ? (present, _binder(r)) : (absent, default));

        public T Bind(Func<IArg, IAccumulator> source) =>
            _binder(source(this));

        public void Inspect(ICollection<IArg> args) => args.Add(this);
    }

    public class ListArg<T> : IArg, IArgBinder<ImmutableArray<T>>
    {
        readonly Arg<T> _arg;

        public ListArg(Arg<T> arg) : this(arg?.Properties, arg) {}

        public ListArg(PropertySet properties, Arg<T> arg)
        {
            _arg = arg ?? throw new ArgumentNullException(nameof(arg));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public PropertySet Properties { get; }

        IArg IArg.WithProperties(PropertySet value) => WithProperties(value);

        public ListArg<T> WithProperties(PropertySet value) =>
            Properties == value ? this : new ListArg<T>(value, _arg);

        public IParser<T> ItemParser => _arg.Parser;

        public IAccumulator CreateAccumulator() => new Accumulator(_arg);

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

    public class TailArg<T> : IArg, IArgBinder<ImmutableArray<T>>
    {
        readonly Arg<T> _arg;

        public TailArg(Arg<T> arg) : this(arg?.Properties, arg) { }

        public TailArg(PropertySet properties, Arg<T> arg)
        {
            _arg = arg ?? throw new ArgumentNullException(nameof(arg));
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
        }

        public PropertySet Properties { get; }

        IArg IArg.WithProperties(PropertySet value) => WithProperties(value);

        public TailArg<T> WithProperties(PropertySet value) =>
            Properties == value ? this : new TailArg<T>(value, _arg);

        public IParser<T> ItemParser => _arg.Parser;

        public IAccumulator CreateAccumulator() => new Accumulator(_arg);

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

    public static class Arg
    {
        public static class Symbols
        {
            public static readonly Symbol Name        = Symbol.New(nameof(Name));
            public static readonly Symbol Description = Symbol.New(nameof(Description));
        }

        public static string Name(this IArg arg) =>
            arg.Properties.Name();

        public static string Name(this PropertySet properties) =>
            (string)properties[Symbols.Name];

        public static T WithName<T>(this T arg, string value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithName(value));

        public static PropertySet WithName(this PropertySet properties, string value) =>
            properties.With(Symbols.Name, value);

        public static string Description(this IArg arg) =>
            (string)arg.Properties[Symbols.Description];

        public static string Description(this PropertySet properties) =>
            (string)properties[Symbols.Description];

        public static IArg WithDescription<T>(this T arg, string value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithDescription(value));

        public static PropertySet WithDescription(this PropertySet properties, string value) =>
            properties.With(Symbols.Description, value);
    }

    public static class Args
    {
        public static Arg<bool> Flag(string name) =>
            new Arg<bool>(Parser.Create<bool>(_ => throw new NotSupportedException()), Accumulator.Flag, r => r.HasValue)
                .WithName(name);

        public static Arg<T> Option<T>(string name, T @default, IParser<T> parser) =>
            new Arg<T>(parser, () => Accumulator.Value(parser), r => r.HasValue ? ((IAccumulator<T>)r).Value : @default)
                .WithName(name);

        public static Arg<T> Option<T>(string name, IParser<T> parser) =>
            Option(name, default, parser);

        public static Arg<T> Arg<T>(string name, T @default, IParser<T> parser) =>
            new Arg<T>(parser, () => Accumulator.Value(parser), r => r.HasValue ? ((IAccumulator<T>)r).Value : @default);

        public static Arg<T> Arg<T>(string name, IParser<T> parser) =>
            Arg(name, default, parser);

        public static TailArg<T> Tail<T>(this Arg<T> arg) =>
            new TailArg<T>(arg);

        public static ListArg<T> List<T>(this Arg<T> arg) =>
            new ListArg<T>(arg);
    }
}
