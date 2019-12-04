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
        IArgData Data { get; }
        IArg WithData(IArgData value);
        IAccumulator CreateAccumulator();
    }

    public interface IArg<out T, D> : IArg, IArgBinder<T> where D : IArgData
    {
        new D Data { get; }
        IArg<T, D> WithData(D value);
        new IAccumulator<T> CreateAccumulator();
    }

    sealed class Arg<T, D> : IArg<T, D> where D : IArgData
    {
        readonly Func<IAccumulator<T>> _accumulatorFactory;
        readonly Func<IAccumulator<T>, T> _binder;

        public Arg(D data, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder)
        {
            Data = data;
            _accumulatorFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public D Data { get; }
        IArgData IArg.Data => Data;

        public Arg<T, D> WithData(D value) =>
            new Arg<T, D>(value, _accumulatorFactory, _binder);

        IArg<T, D> IArg<T, D>.WithData(D value) => WithData(value);
        IArg IArg.WithData(IArgData value) => WithData((D)value);

        IAccumulator IArg.CreateAccumulator() => CreateAccumulator();

        public IAccumulator<T> CreateAccumulator() => _accumulatorFactory();

        object IArgBinder.Bind(Func<IAccumulator> source) => Bind(source);

        public T Bind(Func<IAccumulator> source) =>
            _binder((IAccumulator<T>)source());

        public IEnumerable<IArg> Inspect() { yield return this; }
    }

    public static class Arg
    {
        public static bool IsFlag(this IArg arg) =>
            arg.Data is OptionArgData data && data.Kind == OptionKind.Flag;

        public static bool IsIntegerOption(this IArg arg) =>
            arg.Data is IntegerOptionArgData;

        public static bool IsOperand(this IArg arg) =>
            arg.Data is OperandArgData;

        public static bool IsLiteral(this IArg arg) =>
            arg.Data is LiteralArgData;

        public static string Name(this IArg arg) =>
            arg.Data is OptionArgData data ? data.Name : null;

        public static ShortOptionName ShortName(this IArg arg) =>
            arg.Data is OptionArgData data ? data.ShortName : null;

        public static string Description(this IArg arg) =>
            arg.Data.Description;

        public static IArg<T, OptionArgData> WithName<T>(this IArg<T, OptionArgData> arg, string value) =>
            arg.WithData(arg.Data.WithName(value));

        public static IArg<T, OptionArgData> WithShortName<T>(this IArg<T, OptionArgData> arg, char value) =>
            arg.WithShortName(ShortOptionName.From(value));

        public static IArg<T, OptionArgData> WithShortName<T>(this IArg<T, OptionArgData> arg, ShortOptionName value) =>
            arg.WithData(arg.Data.WithShortName(value));

        public static IArg<T, OptionArgData> WithDescription<T>(this IArg<T, OptionArgData> arg, string value) =>
            arg.WithData(arg.Data.WithDescription(value));

        static IArg<T, D>
            Create<T, D>(D data, Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) where D : IArgData =>
            new Arg<T, D>(data, accumulatorFactory, binder);

        public static readonly IParser<bool> BooleanPlusMinusParser = Parser.Boolean("+", "-");
        public static readonly IParser<int> BinaryPlusMinusParser = from f in BooleanPlusMinusParser select f ? 1 : 0;

        public static IArg<bool, OptionArgData> Flag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Flag(ShortOptionName.From(s[0])),
                _ => Flag(name, null)
            };

        public static IArg<bool, OptionArgData> Flag(char shortName) =>
            Flag(ShortOptionName.From(shortName));

        public static IArg<bool, OptionArgData> Flag(ShortOptionName shortName) =>
            Flag(null, shortName);

        public static IArg<bool, OptionArgData> Flag(string name, ShortOptionName shortName) =>
            Create(new OptionArgData(OptionKind.Flag, name, shortName),
                   () => Accumulator.Value(BooleanPlusMinusParser),
                   r => r.Count > 0);

        public static IArg<int, OptionArgData> CountedFlag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => CountedFlag(ShortOptionName.From(s[0])),
                _ => CountedFlag(name, null)
            };

        public static IArg<int, OptionArgData> CountedFlag(char shortName) =>
            CountedFlag(ShortOptionName.From(shortName));

        public static IArg<int, OptionArgData> CountedFlag(ShortOptionName shortName) =>
            CountedFlag(null, shortName);

        public static IArg<int, OptionArgData> CountedFlag(string name, ShortOptionName shortName) =>
            Create(new OptionArgData(OptionKind.Flag, name, shortName),
                   () => Accumulator.Value(BinaryPlusMinusParser, 0, (acc, f) => acc + f),
                   r => r.GetResult());

        public static IArg<T, OptionArgData> Option<T>(string name, T @default, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), @default, parser),
                _ => Option(name, null, @default, parser)
            };

        public static IArg<T, OptionArgData> Option<T>(char shortName, T @default, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), @default, parser);

        public static IArg<T, OptionArgData> Option<T>(ShortOptionName shortName, T @default, IParser<T> parser) =>
            Option(null, shortName, @default, parser);

        public static IArg<T, OptionArgData> Option<T>(string name, ShortOptionName shortName, T @default, IParser<T> parser) =>
            Create(new OptionArgData(name, shortName), () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<T, OptionArgData> Option<T>(string name, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), parser),
                _ => Option(name, null, parser)
            };

        public static IArg<T, OptionArgData> Option<T>(char shortName, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), parser);

        public static IArg<T, OptionArgData> Option<T>(ShortOptionName shortName, IParser<T> parser) =>
            Option(null, shortName, parser);

        public static IArg<T, OptionArgData> Option<T>(string name, ShortOptionName shortName, IParser<T> parser) =>
            Option(name, shortName, default, parser);

        public static IArg<int, IntegerOptionArgData> IntOpt(string name) =>
            IntOpt(name, -1);

        public static IArg<int, IntegerOptionArgData> IntOpt(string name, int @default) =>
            IntOpt(name, @default, Parser.Int32());

        public static IArg<T, IntegerOptionArgData> IntOpt<T>(string name, IParser<T> parser) =>
            IntOpt(name, default, parser);

        public static IArg<T, IntegerOptionArgData> IntOpt<T>(string name, T @default, IParser<T> parser) =>
            Create(new IntegerOptionArgData(name), () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<T, OperandArgData> Operand<T>(string name, IParser<T> parser) =>
            Operand(name, default, parser);

        public static IArg<T, OperandArgData> Operand<T>(string name, T @default, IParser<T> parser) =>
            Create(new OperandArgData(name), () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<string, LiteralArgData> Literal(string value) =>
            Literal(value, StringComparison.Ordinal);

        public static IArg<string, LiteralArgData> Literal(string value, StringComparison comparison)
        {
            var parser = Parser.Literal(value, comparison);
            return Create(new LiteralArgData(value), () => Accumulator.Value(parser), r => r.GetResult());
        }

        public static IArg<ImmutableArray<T>, OperandArgData> List<T>(this IArg<T, OperandArgData> arg) =>
            List<T, OperandArgData>(arg);

        public static IArg<ImmutableArray<T>, OptionArgData> List<T>(this IArg<T, OptionArgData> arg) =>
            List<T, OptionArgData>(arg);

        public static IArg<(bool Present, T Value), D>
            FlagPresence<T, D>(this IArg<T, D> arg) where D : IArgData =>
            arg.FlagPresence(false, true);

        public static IArg<(P Presence, T Value), D>
            FlagPresence<T, P, D>(this IArg<T, D> arg, P absent, P present)
            where D : IArgData =>
            Create(arg.Data,
                   () => from v in arg.CreateAccumulator()
                         select (Presence: present, Value: v),
                   r => r.Count > 0 ? (present, arg.Bind(() => Accumulator.Return(r.GetResult().Item2))) : (absent, default));

        static IArg<ImmutableArray<T>, D> List<T, D>(IArg<T, D> arg)
            where D : IArgData =>
            Create(arg.Data,
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

        public static IArg<ImmutableArray<T>, D> Tail<T, D>(this IArg<T, D> arg)
            where D : IArgData =>
            Create(arg.Data,
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
                   r => r.GetResult());
    }
}
