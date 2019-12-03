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
        IAccumulator CreateAccumulator();

        string Description { get; }
        IArg WithDescription(string value);
    }

    public interface IArgTrait {}
    public interface IOperandArg        : IArgTrait       {}
    public interface ILiteralArg        : IArgTrait       {}
    public interface ITailArg           : IArgTrait       {}

    public interface IOptionArg : IArg
    {
        bool IsFlag { get;}
        new IOptionArg WithDescription(string value);
    }

    public interface IOptionArg<out T> : IArg, IArgBinder<T>
    {
        new IAccumulator<T> CreateAccumulator();
        new IOptionArg<T> WithDescription(string value);
    }

    public interface INamedOptionArg : IOptionArg
    {
        string Name { get; }
        ShortOptionName ShortName { get; }
        new INamedOptionArg WithDescription(string value);
        INamedOptionArg WithName(string value);
        INamedOptionArg WithShortName(ShortOptionName value);
    }

    public interface INamedOptionArg<out T> : INamedOptionArg, IOptionArg<T>
    {
        new INamedOptionArg<T> WithDescription(string value);
        new INamedOptionArg<T> WithName(string value);
        new INamedOptionArg<T> WithShortName(ShortOptionName value);
    }

    public interface IFlagOptionArg : INamedOptionArg<bool>
    {
        new IFlagOptionArg WithDescription(string value);
        new IFlagOptionArg WithName(string value);
        new IFlagOptionArg WithShortName(ShortOptionName value);
    }

    public interface IIntOptArg : IOptionArg, IOptionArg<int>
    {
        new IIntOptArg WithDescription(string value);
    }

    sealed class NamedOptionArg<T> : INamedOptionArg<T>
    {
        object IArgBinder.Bind(Func<IAccumulator> source)
        {
            return Bind(source);
        }

        public T Bind(Func<IAccumulator> source)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IArg> Inspect()
        {
            throw new NotImplementedException();
        }

        IAccumulator IArg.CreateAccumulator()
        {
            return CreateAccumulator();
        }

        INamedOptionArg<T> INamedOptionArg<T>.WithDescription(string value)
        {
            throw new NotImplementedException();
        }

        public INamedOptionArg<T> WithName(string value)
        {
            throw new NotImplementedException();
        }

        public INamedOptionArg<T> WithShortName(ShortOptionName value)
        {
            throw new NotImplementedException();
        }

        IOptionArg<T> IOptionArg<T>.WithDescription(string value)
        {
            throw new NotImplementedException();
        }

        public IAccumulator<T> CreateAccumulator()
        {
            throw new NotImplementedException();
        }

        public string Description { get; }
        INamedOptionArg INamedOptionArg.WithDescription(string value)
        {
            throw new NotImplementedException();
        }

        INamedOptionArg INamedOptionArg.WithName(string value)
        {
            return WithName(value);
        }

        INamedOptionArg INamedOptionArg.WithShortName(ShortOptionName value)
        {
            return WithShortName(value);
        }

        public string Name { get; }
        public ShortOptionName ShortName { get; }

        IOptionArg IOptionArg.WithDescription(string value)
        {
            throw new NotImplementedException();
        }

        public bool IsFlag { get; }

        IArg IArg.WithDescription(string value)
        {
            throw new NotImplementedException();
        }
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
            public static readonly Symbol Trait       = Symbol.New(nameof(Trait));
        }

        public static bool IsFlag(this IArg arg) =>
            arg.Properties.IsFlag();

        public static bool IsFlag(this PropertySet properties) =>
            properties[Symbols.Trait] is IFlagOptionArg;

        public static bool IsIntOpt(this IArg arg) =>
            arg.Properties.IsIntOpt();

        public static bool IsIntOpt(this PropertySet properties) =>
            properties[Symbols.Trait] is IIntOptArg;

        public static string Name(this IArg arg) =>
            arg.Properties.Name();

        public static string Name(this PropertySet properties) =>
            (string)properties[Symbols.Name];

        public static IArg<T, V, A> WithName<T, V, A>(this IArg<T, V, A> arg, string value) where A : INamedOptionArg =>
            arg.WithProperties(arg.Properties.WithName(value));

        public static PropertySet WithName(this PropertySet properties, string value) =>
            properties.Set(Symbols.Name, value);

        public static ShortOptionName ShortName(this IArg arg) =>
            arg.Properties.ShortName();

        public static ShortOptionName ShortName(this PropertySet properties) =>
            (ShortOptionName)properties[Symbols.ShortName];

        public static IArg<T, V, A> WithShortName<T, V, A>(this IArg<T, V, A> arg, char value) where A : INamedOptionArg =>
            arg.WithShortName(ShortOptionName.From(value));

        public static IArg<T, V, A> WithShortName<T, V, A>(this IArg<T, V, A> arg, ShortOptionName value) where A : INamedOptionArg =>
            arg.WithProperties(arg.Properties.WithShortName(value));

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
        static readonly INamedOptionArg NamedOptionArg = new Traits.NamedOptionArg();
        static readonly IFlagOptionArg  FlagOptionArg  = new Traits.FlagOptionArg();
        static readonly IIntOptArg      IntOptArg      = new Traits.IntOptArg();
        static readonly IOperandArg     OperandArg     = new Traits.OperandArg();
        static readonly ILiteralArg     LiteralArg     = new Traits.LiteralArg();
        static readonly ITailArg        TailArg        = new Traits.TailArg();

        static class Traits
        {
            public struct NamedOptionArg   : INamedOptionArg {}
            public struct FlagOptionArg    : IFlagOptionArg  {}
            public struct IntOptArg        : IIntOptArg      {}
            public struct OperandArg       : IOperandArg     {}
            public struct LiteralArg       : ILiteralArg     {}
            public struct TailArg          : ITailArg        {}
        }

        static IArg<T, V, A>
            Create<T, V, A>(A _, IParser<V> parser,
                         Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) =>
            Create(_, PropertySet.Empty, parser, accumulatorFactory, binder);

        static IArg<T, V, A>
            Create<T, V, A>(A _, PropertySet properties, IParser<V> parser,
                         Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) =>
            new Arg<T, V, A>(properties.Set(Symbols.Trait, _), parser, accumulatorFactory, binder);

        public static readonly IParser<bool> BooleanPlusMinusParser = Parser.Boolean("+", "-");
        public static readonly IParser<int> BinaryPlusMinusParser = from f in BooleanPlusMinusParser select f ? 1 : 0;

        public static IArg<bool, bool, IFlagOptionArg> Flag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Flag(ShortOptionName.From(s[0])),
                _ => Flag(name, null)
            };

        public static IArg<bool, bool, IFlagOptionArg> Flag(char shortName) =>
            Flag(ShortOptionName.From(shortName));

        public static IArg<bool, bool, IFlagOptionArg> Flag(ShortOptionName shortName) =>
            Flag(null, shortName);

        public static IArg<bool, bool, IFlagOptionArg> Flag(string name, ShortOptionName shortName) =>
            Create(FlagOptionArg, BooleanPlusMinusParser,
                   () => Accumulator.Value(BooleanPlusMinusParser),
                   r => r.Count > 0)
                .WithName(name)
                .WithShortName(shortName);

        public static IArg<int, int, IFlagOptionArg> CountedFlag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => CountedFlag(ShortOptionName.From(s[0])),
                _ => CountedFlag(name, null)
            };

        public static IArg<int, int, IFlagOptionArg> CountedFlag(char shortName) =>
            CountedFlag(ShortOptionName.From(shortName));

        public static IArg<int, int, IFlagOptionArg> CountedFlag(ShortOptionName shortName) =>
            CountedFlag(null, shortName);

        public static IArg<int, int, IFlagOptionArg> CountedFlag(string name, ShortOptionName shortName) =>
            Create(FlagOptionArg, BinaryPlusMinusParser,
                   () => Accumulator.Value(BinaryPlusMinusParser, 0, (acc, f) => acc + f),
                   r => r.GetResult())
                .WithName(name)
                .WithShortName(shortName);

        public static IArg<T, T, INamedOptionArg> Option<T>(string name, T @default, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), @default, parser),
                _ => Option(name, null, @default, parser)
            };

        public static IArg<T, T, INamedOptionArg> Option<T>(char shortName, T @default, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), @default, parser);

        public static IArg<T, T, INamedOptionArg> Option<T>(ShortOptionName shortName, T @default, IParser<T> parser) =>
            Option(null, shortName, @default, parser);

        public static IArg<T, T, INamedOptionArg> Option<T>(string name, ShortOptionName shortName, T @default, IParser<T> parser) =>
            Create(NamedOptionArg, parser, () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default)
                .WithName(name)
                .WithShortName(shortName);

        public static IArg<T, T, INamedOptionArg> Option<T>(string name, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), parser),
                _ => Option(name, null, parser)
            };

        public static IArg<T, T, INamedOptionArg> Option<T>(char shortName, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), parser);

        public static IArg<T, T, INamedOptionArg> Option<T>(ShortOptionName shortName, IParser<T> parser) =>
            Option(null, shortName, parser);

        public static IArg<T, T, INamedOptionArg> Option<T>(string name, ShortOptionName shortName, IParser<T> parser) =>
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

        public static IArg<ImmutableArray<T>, T, IOperandArg> List<T>(this IArg<T, T, IOperandArg> arg) =>
            List<T, IOperandArg>(arg);

        public static IArg<ImmutableArray<T>, T, IOptionArg> List<T>(this IArg<T, T, IOptionArg> arg) =>
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

        static IArg<ImmutableArray<T>, T, A> List<T, A>(IArg<T, T, A> arg) =>
            Create(default(A),
                   arg.Properties,
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
                       r => r.GetResult());

        public static IArg<ImmutableArray<T>, T, ITailArg> Tail<T, A>(this IArg<T, T, A> arg)
            where A : IOperandArg =>
            Create(TailArg,
                   arg.Properties,
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
    }
}
