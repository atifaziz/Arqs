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

    public interface IArgTrait   {}
    public interface IOptionArg  : IArgTrait {}
    public interface IIntOptArg  : IArgTrait {}
    public interface IOperandArg : IArgTrait {}
    public interface ILiteralArg : IArgTrait {}
    public interface ITailArg    : IArgTrait {}
    public interface IListArg    : IArgTrait {}

    public interface IArg<out T, V, A> : IArg, IArgBinder<T>
    {
        new IParser<V> Parser { get; }
        new IArg<T, V, A> WithProperties(PropertySet value);
        new IAccumulator<T> CreateAccumulator();
    }

    interface IArgVisitable
    {
        T Accept<T>(IArgVisitor<T> visitor);
    }

    interface IArgVisitor<out R>
    {
        R Visit<T, V, A>(IArg<T, V, A> arg);
    }

    sealed class Arg<T, V, A> : IArg<T, V, A>, IArgVisitable
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

        R IArgVisitable.Accept<R>(IArgVisitor<R> visitor) => visitor.Visit(this);

        public IAccumulator<T> CreateAccumulator() => _accumulatorFactory();

        object IArgBinder.Bind(Func<IAccumulator> source) => Bind(source);

        public T Bind(Func<IAccumulator> source) =>
            _binder((IAccumulator<T>)source());

        public IEnumerable<IArg> Inspect() { yield return this; }
    }

    public static partial class Arg
    {
        static class Symbols
        {
            public static readonly Symbol Name        = Symbol.New(nameof(Name));
            public static readonly Symbol ShortName   = Symbol.New(nameof(ShortName));
            public static readonly Symbol Description = Symbol.New(nameof(Description));
            public static readonly Symbol IsFlag      = Symbol.New(nameof(IsFlag));
        }

        public static bool IsFlag(this IArg arg) =>
            arg.Properties.IsFlag();

        public static bool IsFlag(this PropertySet properties) =>
            (bool?)properties[Symbols.IsFlag] ?? false;

        public static T WithIsFlag<T>(this T arg, bool value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithIsFlag(value));

        public static PropertySet WithIsFlag(this PropertySet properties, bool value) =>
            properties.Set(Symbols.IsFlag, value);

        public static string Name(this IArg arg) =>
            arg.Properties.Name();

        public static string Name(this PropertySet properties) =>
            (string)properties[Symbols.Name];

        public static T WithName<T>(this T arg, string value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithName(value));

        public static PropertySet WithName(this PropertySet properties, string value) =>
            properties.Set(Symbols.Name, value);

        public static ShortOptionName ShortName(this IArg arg) =>
            arg.Properties.ShortName();

        public static ShortOptionName ShortName(this PropertySet properties) =>
            (ShortOptionName)properties[Symbols.ShortName];

        public static T WithShortName<T>(this T arg, char value) where T : IArg =>
            arg.WithShortName(ShortOptionName.From(value));

        public static T WithShortName<T>(this T arg, ShortOptionName value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithShortName(value));

        public static PropertySet WithShortName(this PropertySet properties, char value) =>
            properties.WithShortName(ShortOptionName.From(value));

        public static PropertySet WithShortName(this PropertySet properties, ShortOptionName value) =>
            properties.Set(Symbols.ShortName, value);

        public static string Description(this IArg arg) =>
            (string)arg.Properties[Symbols.Description];

        public static string Description(this PropertySet properties) =>
            (string)properties[Symbols.Description];

        public static T WithDescription<T>(this T arg, string value) where T : IArg =>
            (T)arg.WithProperties(arg.Properties.WithDescription(value));

        public static PropertySet WithDescription(this PropertySet properties, string value) =>
            properties.Set(Symbols.Description, value);
    }

    public partial class Arg
    {
        static readonly IOptionArg OptionArg   = null;
        static readonly IIntOptArg IntOptArg   = null;
        static readonly IOperandArg OperandArg = null;
        static readonly ILiteralArg LiteralArg = null;
        static readonly IListArg ListArg       = null;
        static readonly ITailArg TailArg       = null;

        static IArg<T, V, A>
            Create<T, V, A>(A _, IParser<V> parser,
                         Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) =>
            new Arg<T, V, A>(parser, accumulatorFactory, binder);

        public static readonly IParser<bool> BooleanPlusMinusParser = Parser.Boolean("+", "-");
        public static readonly IParser<int> BinaryPlusMinusParser = from f in BooleanPlusMinusParser select f ? 1 : 0;

        public static IArg<bool, bool, IOptionArg> Flag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Flag(ShortOptionName.From(s[0])),
                _ => Flag(name, null)
            };

        public static IArg<bool, bool, IOptionArg> Flag(char shortName) =>
            Flag(ShortOptionName.From(shortName));

        public static IArg<bool, bool, IOptionArg> Flag(ShortOptionName shortName) =>
            Flag(null, shortName);

        public static IArg<bool, bool, IOptionArg> Flag(string name, ShortOptionName shortName) =>
            Create(OptionArg, BooleanPlusMinusParser,
                   () => Accumulator.Value(BooleanPlusMinusParser),
                   r => r.Count > 0)
                .WithName(name)
                .WithShortName(shortName)
                .WithIsFlag(true);


        public static IArg<int, int, IOptionArg> CountedFlag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => CountedFlag(ShortOptionName.From(s[0])),
                _ => CountedFlag(name, null)
            };

        public static IArg<int, int, IOptionArg> CountedFlag(char shortName) =>
            CountedFlag(ShortOptionName.From(shortName));

        public static IArg<int, int, IOptionArg> CountedFlag(ShortOptionName shortName) =>
            CountedFlag(null, shortName);

        public static IArg<int, int, IOptionArg> CountedFlag(string name, ShortOptionName shortName) =>
            Create(OptionArg, BinaryPlusMinusParser,
                   () => Accumulator.Value(BinaryPlusMinusParser, 0, (acc, f) => acc + f),
                   r => r.GetResult())
                .WithName(name)
                .WithShortName(shortName)
                .WithIsFlag(true);

        public static IArg<T, T, IOptionArg> Option<T>(string name, T @default, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), @default, parser),
                _ => Option(name, null, @default, parser)
            };

        public static IArg<T, T, IOptionArg> Option<T>(char shortName, T @default, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), @default, parser);

        public static IArg<T, T, IOptionArg> Option<T>(ShortOptionName shortName, T @default, IParser<T> parser) =>
            Option(null, shortName, @default, parser);

        public static IArg<T, T, IOptionArg> Option<T>(string name, ShortOptionName shortName, T @default, IParser<T> parser) =>
            Create(OptionArg, parser, () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default)
                .WithName(name)
                .WithShortName(shortName);

        public static IArg<T, T, IOptionArg> Option<T>(string name, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), parser),
                _ => Option(name, null, parser)
            };

        public static IArg<T, T, IOptionArg> Option<T>(char shortName, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), parser);

        public static IArg<T, T, IOptionArg> Option<T>(ShortOptionName shortName, IParser<T> parser) =>
            Option(null, shortName, parser);

        public static IArg<T, T, IOptionArg> Option<T>(string name, ShortOptionName shortName, IParser<T> parser) =>
            Option(name, shortName, default, parser);

        public static IArg<int, int, IIntOptArg> IntOpt(string name) =>
            IntOpt(name, -1);

        public static IArg<int, int, IIntOptArg> IntOpt(string name, int @default) =>
            IntOpt(name, @default, Parser.Int32());

        public static IArg<T, T, IIntOptArg> IntOpt<T>(string name, IParser<T> parser) =>
            IntOpt(name, default, parser);

        public static IArg<T, T, IIntOptArg> IntOpt<T>(string name, T @default, IParser<T> parser) =>
            Create(IntOptArg, parser, () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<T, T, IOperandArg> Operand<T>(string name, IParser<T> parser) =>
            Operand(name, default, parser);

        public static IArg<T, T, IOperandArg> Operand<T>(string name, T @default, IParser<T> parser) =>
            Create(OperandArg, parser, () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<string, string, ILiteralArg> Literal(string value) =>
            Literal(value, StringComparison.Ordinal);

        public static IArg<string, string, ILiteralArg> Literal(string value, StringComparison comparison)
        {
            var parser = Parser.Literal(value, comparison);
            return Create(LiteralArg, parser, () => Accumulator.Value(parser), r => r.GetResult());
        }

        public static IArg<ImmutableArray<T>, T, IListArg> List<T>(this IArg<T, T, IOperandArg> arg) =>
            List<T, IOperandArg>(arg);

        public static IArg<ImmutableArray<T>, T, IListArg> List<T>(this IArg<T, T, IOptionArg> arg) =>
            List<T, IOptionArg>(arg);

        public static IArg<(bool Present, T Value), (bool Present, T Value), A>
            FlagPresence<T, A>(this IArg<T, T, A> arg) => arg.FlagPresence(false, true);

        public static IArg<(P Presence, T Value), (P Presence, T Value), A>
            FlagPresence<T, P, A>(this IArg<T, T, A> arg, P absent, P present) =>
            Create(default(A),
                   from v in arg.Parser select (present, v),
                   () => from v in arg.CreateAccumulator()
                         select (Presence: present, Value: v),
                   r => r.Count > 0 ? (present, arg.Bind(() => Accumulator.Return(r.GetResult().Item2))) : (absent, default))
                .WithProperties(arg.Properties);

        static IArg<ImmutableArray<T>, T, IListArg> List<T, A>(IArg<T, T, A> arg) =>
            Create(ListArg,
                   arg.Parser,
                   () =>
                       Accumulator.Create(ImmutableArray.CreateBuilder<T>(),
                           (array, args) =>
                           {
                               var accumulator = arg.CreateAccumulator();
                               if (!accumulator.Read(args))
                                   return default;
                               array.Add(arg.Bind(() => accumulator));
                               return ParseResult.Success(array);
                           },
                           a => a.ToImmutable()),
                       r => r.GetResult())
                .WithProperties(arg.Properties);

        public static IArg<ImmutableArray<T>, T, ITailArg> Tail<T, A>(this IArg<T, T, A> arg)
            where A : IOperandArg =>
            Create(TailArg,
                   arg.Parser,
                   () =>
                       Accumulator.Create(ImmutableArray<T>.Empty,
                           (seed, args) =>
                           {
                               var array = seed.ToBuilder();
                               while (args.HasMore())
                               {
                                   var accumulator = arg.CreateAccumulator();
                                   if (!accumulator.Read(args))
                                       return default;
                                   array.Add(arg.Bind(() => accumulator));
                               }
                               return ParseResult.Success(array.ToImmutable());
                           },
                           r => r),
                   r => r.GetResult())
                .WithProperties(arg.Properties);

        public static bool IsOption (this IArg arg) => arg.Is<IOptionArg >();
        public static bool IsIntOpt (this IArg arg) => arg.Is<IIntOptArg >();
        public static bool IsOperand(this IArg arg) => arg.Is<IOperandArg>();
        public static bool IsLiteral(this IArg arg) => arg.Is<ILiteralArg>();
        public static bool IsTail   (this IArg arg) => arg.Is<ITailArg   >();
        public static bool IsList   (this IArg arg) => arg.Is<IListArg   >();

        static bool Is<T>(this IArg arg) where T : IArgTrait =>
            ((IArgVisitable)arg).Accept(ArgTypeVisitor.Instance) is var (_, _, t) && t == typeof(T);

        sealed class ArgTypeVisitor : IArgVisitor<(Type, Type, Type)>
        {
            public static readonly IArgVisitor<(Type, Type, Type)> Instance = new ArgTypeVisitor();

            ArgTypeVisitor() {}

            (Type, Type, Type) IArgVisitor<(Type, Type, Type)>.Visit<T, V, A>(IArg<T, V, A> _) =>
                (typeof(T), typeof(V), typeof(A));
        }
    }
}
