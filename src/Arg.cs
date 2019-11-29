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

    public interface IArg : IArgBinder
    {
        PropertySet Properties { get; }
        IArg WithProperties(PropertySet value);
        IAccumulator CreateAccumulator();
    }

    public interface IArg<out T> : IArg, IArgBinder<T>
    {
        new IArg<T> WithProperties(PropertySet value);
        new IAccumulator<T> CreateAccumulator();
    }

    public class Arg<T> : IArg<T>
    {
        readonly Func<IAccumulator<T>> _accumulatorFactory;
        readonly Func<IAccumulator<T>, T> _binder;

        public Arg(IParser<T> parser, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder) :
            this(PropertySet.Empty, parser, accumulatorFactory, binder) {}

        public Arg(PropertySet properties, IParser<T> parser, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _accumulatorFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public PropertySet Properties { get; }

        IArg IArg.WithProperties(PropertySet value) => WithProperties(value);
        IArg<T> IArg<T>.WithProperties(PropertySet value) => WithProperties(value);

        public Arg<T> WithProperties(PropertySet value) =>
            Properties == value ? this : new Arg<T>(value, Parser, _accumulatorFactory, _binder);

        public IParser<T> Parser { get; }

        IAccumulator IArg.CreateAccumulator() => CreateAccumulator();
        public IAccumulator<T> CreateAccumulator() => _accumulatorFactory();

        public Arg<(bool Present, T Value)> FlagPresence() =>
            FlagPresence(false, true);

        public Arg<(TPresence Presence, T Value)> FlagPresence<TPresence>(TPresence absent, TPresence present) =>
            new Arg<(TPresence, T)>(Properties,
                                    from v in Parser select (present, v),
                                    () => from v in _accumulatorFactory()
                                          select (Presence: present, Value: v),
                                    r => r.HasValue ? (present, _binder(Accumulator.Return(r.Value.Item2))) : (absent, default));

        object IArgBinder.Bind(Func<IArg, IAccumulator> source) => Bind(source);

        public T Bind(Func<IArg, IAccumulator> source) =>
            _binder((IAccumulator<T>)source(this));

        public IEnumerable<IArg> Inspect() { yield return this; }
    }

    public class ListArg<T> : IArg<ImmutableArray<T>>
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
        IArg<ImmutableArray<T>> IArg<ImmutableArray<T>>.WithProperties(PropertySet value) => WithProperties(value);

        public ListArg<T> WithProperties(PropertySet value) =>
            Properties == value ? this : new ListArg<T>(value, _arg);

        public IParser<T> ItemParser => _arg.Parser;

        IAccumulator IArg.CreateAccumulator() => CreateAccumulator();

        public IAccumulator<ImmutableArray<T>> CreateAccumulator() =>
            Accumulator.Create(ImmutableArray<T>.Empty, (seed, arg) =>
            {
                var array = seed.ToBuilder();
                var reader = _arg.CreateAccumulator();
                if (!reader.Read(arg))
                    return default;
                array.Add(_arg.Bind(_ => reader));
                return ParseResult.Success(array.ToImmutable());
            });

        object IArgBinder.Bind(Func<IArg, IAccumulator> source) => Bind(source);

        public ImmutableArray<T> Bind(Func<IArg, IAccumulator> source) =>
            (ImmutableArray<T>)source(this).Value;

        public IEnumerable<IArg> Inspect() { yield return this; }
    }

    public class TailArg<T> : IArg<ImmutableArray<T>>
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
        IArg<ImmutableArray<T>> IArg<ImmutableArray<T>>.WithProperties(PropertySet value) => WithProperties(value);

        public TailArg<T> WithProperties(PropertySet value) =>
            Properties == value ? this : new TailArg<T>(value, _arg);

        public IParser<T> ItemParser => _arg.Parser;

        IAccumulator IArg.CreateAccumulator() => CreateAccumulator();

        public IAccumulator<ImmutableArray<T>> CreateAccumulator() =>
            Accumulator.Create(ImmutableArray<T>.Empty, (seed, arg) =>
            {
                var array = seed.ToBuilder();
                while (arg.HasMore())
                {
                    var reader = _arg.CreateAccumulator();
                    if (!reader.Read(arg))
                        return default;
                    array.Add(_arg.Bind(_ => reader));
                }
                return ParseResult.Success(array.ToImmutable());
            });

        object IArgBinder.Bind(Func<IArg, IAccumulator> source) => Bind(source);

        public ImmutableArray<T> Bind(Func<IArg, IAccumulator> source) =>
            ((ImmutableArray<T>)source(this).Value);

        public IEnumerable<IArg> Inspect() { yield return this; }
    }

    public static partial class Arg
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

    public partial class Arg
    {
        public static Arg<T> Create<T>(IParser<T> parser,
                                       Func<IAccumulator<T>> accumulatorFactory,
                                       Func<IAccumulator<T>, T> binder) =>
            new Arg<T>(parser, accumulatorFactory, binder);

        public static Arg<bool> Flag(string name) =>
            Create(Parser.Create<bool>(_ => throw new NotSupportedException()),
                                       () => from x in Accumulator.Flag() select x > 0,
                                       r => r.HasValue)
                .WithName(name);

        public static Arg<T> Option<T>(string name, T @default, IParser<T> parser) =>
            Create(parser, () => Accumulator.Value(parser), r => r.HasValue ? r.Value : @default)
                .WithName(name);

        public static Arg<T> Option<T>(string name, IParser<T> parser) =>
            Option(name, default, parser);

        public static Arg<T> Operand<T>(string name, T @default, IParser<T> parser) =>
            Create(parser, () => Accumulator.Value(parser), r => r.HasValue ? r.Value : @default);

        public static Arg<T> Operand<T>(string name, IParser<T> parser) =>
            Operand(name, default, parser);

        public static TailArg<T> Tail<T>(this Arg<T> arg) =>
            new TailArg<T>(arg);

        public static ListArg<T> List<T>(this Arg<T> arg) =>
            new ListArg<T>(arg);
    }
}
