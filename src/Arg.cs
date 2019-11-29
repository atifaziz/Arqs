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
        IParser Parser { get; }
        IAccumulator CreateAccumulator();
    }

    public interface IFlagArg    {}
    public interface IOptionArg  {}
    public interface IOperandArg {}
    public interface ILiteralArg {}
    public interface ITailArg    {}
    public interface IListArg    {}

    public interface IArg<out T, V, A> : IArg, IArgBinder<T>
    {
        new IParser<V> Parser { get; }
        new IArg<T, V, A> WithProperties(PropertySet value);
        new IAccumulator<T> CreateAccumulator();
    }

    sealed class Arg<T, V, A> : IArg<T, V, A>
    {
        readonly Func<IAccumulator<T>> _accumulatorFactory;
        readonly Func<IAccumulator<T>, T> _binder;

        public Arg(IParser<V> parser, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder) :
            this(PropertySet.Empty, parser, accumulatorFactory, binder) {}

        public Arg(PropertySet properties, IParser<V> parser, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder)
        {
            Properties = properties ?? throw new ArgumentNullException(nameof(properties));
            _accumulatorFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            Parser = parser ?? throw new ArgumentNullException(nameof(parser));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public PropertySet Properties { get; }

        IArg IArg.WithProperties(PropertySet value) => WithProperties(value);
        IArg<T, V, A> IArg<T, V, A>.WithProperties(PropertySet value) => WithProperties(value);

        public Arg<T, V, A> WithProperties(PropertySet value) =>
            Properties == value ? this : new Arg<T, V, A>(value, Parser, _accumulatorFactory, _binder);

        IParser IArg.Parser => Parser;

        public IParser<V> Parser { get; }

        IAccumulator IArg.CreateAccumulator() => CreateAccumulator();
        public IAccumulator<T> CreateAccumulator() => _accumulatorFactory();

        object IArgBinder.Bind(Func<IArg, IAccumulator> source) => Bind(source);

        public T Bind(Func<IArg, IAccumulator> source) =>
            _binder((IAccumulator<T>)source(this));

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
        static readonly IOptionArg OptionArg   = null;
        static readonly IFlagArg FlagArg       = null;
        static readonly IOperandArg OperandArg = null;
        static readonly ILiteralArg LiteralArg = null;
        static readonly IListArg ListArg       = null;
        static readonly ITailArg TailArg       = null;

        static IArg<T, V, A>
            Create<T, V, A>(A _, IParser<V> parser,
                         Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) =>
            new Arg<T, V, A>(parser, accumulatorFactory, binder);

        public static IArg<bool, bool, IFlagArg> Flag(string name) =>
            Create(FlagArg,
                   Parser.Create<bool>(_ => throw new NotSupportedException()),
                   () => from x in Accumulator.Flag() select x > 0,
                   r => r.HasValue)
                .WithName(name);

        public static IArg<T, T, IOptionArg> Option<T>(string name, T @default, IParser<T> parser) =>
            Create(OptionArg, parser, () => Accumulator.Value(parser), r => r.HasValue ? r.Value : @default)
                .WithName(name);

        public static IArg<T, T, IOptionArg> Option<T>(string name, IParser<T> parser) =>
            Option(name, default, parser);

        public static IArg<T, T, IOperandArg> Operand<T>(string name, IParser<T> parser) =>
            Operand(name, default, parser);

        public static IArg<T, T, IOperandArg> Operand<T>(string name, T @default, IParser<T> parser) =>
            Create(OperandArg, parser, () => Accumulator.Value(parser), r => r.HasValue ? r.Value : @default);

        public static IArg<string, string, ILiteralArg> Literal(string value) =>
            Literal(value, StringComparison.Ordinal);

        public static IArg<string, string, ILiteralArg> Literal(string value, StringComparison comparison)
        {
            var parser = Parser.Literal(value, comparison);
            return Create(LiteralArg, parser, () => Accumulator.Value(parser), r => r.Value);
        }

        public static IArg<ImmutableArray<T>, T, IListArg> List<T>(this IArg<T, T, IOperandArg> arg) =>
            List<T, IOperandArg>(arg);

        public static IArg<ImmutableArray<T>, T, IListArg> List<T>(this IArg<T, T, IOptionArg> arg) =>
            List<T, IOptionArg>(arg);

        public static IArg<ImmutableArray<T>, T, IListArg> List<T>(this IArg<T, T, IFlagArg> arg) =>
            List<T, IFlagArg>(arg);

        public static IArg<(bool Present, T Value), (bool Present, T Value), A>
            FlagPresence<T, A>(this IArg<T, T, A> arg) => arg.FlagPresence(false, true);

        public static IArg<(P Presence, T Value), (P Presence, T Value), A>
            FlagPresence<T, P, A>(this IArg<T, T, A> arg, P absent, P present) =>
            Create(default(A),
                   from v in arg.Parser select (present, v),
                   () => from v in arg.CreateAccumulator()
                         select (Presence: present, Value: v),
                   r => r.HasValue ? (present, arg.Bind(_ => Accumulator.Return(r.Value.Item2))) : (absent, default))
                .WithProperties(arg.Properties);

        static IArg<ImmutableArray<T>, T, IListArg> List<T, A>(IArg<T, T, A> arg) =>
            Create(ListArg,
                   arg.Parser,
                   () => Accumulator.Create(ImmutableArray<T>.Empty, (seed, args) =>
                   {
                       var array = seed.ToBuilder();
                       var accumulator = arg.CreateAccumulator();
                       if (!accumulator.Read(args))
                           return default;
                       array.Add(arg.Bind(_ => accumulator));
                       return ParseResult.Success(array.ToImmutable());
                   }),
                   r => r.Value)
                .WithProperties(arg.Properties);

        public static IArg<ImmutableArray<T>, T, ITailArg> Tail<T, A>(this IArg<T, T, A> arg)
            where A : IOperandArg =>
            Create(TailArg,
                   arg.Parser,
                   () => Accumulator.Create(ImmutableArray<T>.Empty, (seed, args) =>
                   {
                       var array = seed.ToBuilder();
                       while (args.HasMore())
                       {
                           var reader = arg.CreateAccumulator();
                           if (!reader.Read(args))
                               return default;
                           array.Add(arg.Bind(_ => reader));
                       }
                       return ParseResult.Success(array.ToImmutable());
                   }),
                   r => r.Value)
                .WithProperties(arg.Properties);
    }
}
