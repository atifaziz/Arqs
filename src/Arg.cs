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
        IArgInfo Info { get; }
        IArg WithInfo(IArgInfo value);
        IAccumulator CreateAccumulator();
    }

    public interface IArg<out T, TInfo> : IArg, IArgBinder<T> where TInfo : IArgInfo
    {
        new TInfo Info { get; }
        IArg<T, TInfo> WithInfo(TInfo value);
        new IAccumulator<T> CreateAccumulator();
    }

    sealed class Arg<T, TInfo> : IArg<T, TInfo> where TInfo : IArgInfo
    {
        readonly Func<IAccumulator<T>> _accumulatorFactory;
        readonly Func<IAccumulator<T>, T> _binder;

        public Arg(TInfo data, Func<IAccumulator<T>> accumulatorFactory, Func<IAccumulator<T>, T> binder)
        {
            Info = data;
            _accumulatorFactory = accumulatorFactory ?? throw new ArgumentNullException(nameof(accumulatorFactory));
            _binder = binder ?? throw new ArgumentNullException(nameof(binder));
        }

        public TInfo Info { get; }
        IArgInfo IArg.Info => Info;

        public Arg<T, TInfo> WithData(TInfo value) =>
            new Arg<T, TInfo>(value, _accumulatorFactory, _binder);

        IArg<T, TInfo> IArg<T, TInfo>.WithInfo(TInfo value) => WithData(value);
        IArg IArg.WithInfo(IArgInfo value) => WithData((TInfo)value);

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
            arg.Info is OptionArgInfo data && data.ArgKind == OptionArgKind.Flag;

        public static bool IsIntegerOption(this IArg arg) =>
            arg.Info is IntegerOptionArgInfo;

        public static bool IsOperand(this IArg arg) =>
            arg.Info is OperandArgInfo;

        public static bool IsLiteral(this IArg arg) =>
            arg.Info is LiteralArgInfo;

        public static string Name(this IArg arg) =>
            arg.Info is OptionArgInfo data ? data.Name : null;

        public static ShortOptionName ShortName(this IArg arg) =>
            arg.Info is OptionArgInfo data ? data.ShortName : null;

        public static string Description(this IArg arg) =>
            arg.Info.Description;

        public static IArg<T, OptionArgInfo> WithName<T>(this IArg<T, OptionArgInfo> arg, string value) =>
            arg.WithInfo(arg.Info.WithName(value));

        public static IArg<T, OptionArgInfo> WithShortName<T>(this IArg<T, OptionArgInfo> arg, char value) =>
            arg.WithShortName(ShortOptionName.From(value));

        public static IArg<T, OptionArgInfo> WithShortName<T>(this IArg<T, OptionArgInfo> arg, ShortOptionName value) =>
            arg.WithInfo(arg.Info.WithShortName(value));

        public static IArg<T, OptionArgInfo> WithDescription<T>(this IArg<T, OptionArgInfo> arg, string value) =>
            arg.WithInfo(arg.Info.WithDescription(value));

        public static IArg<T, OptionArgInfo> WithIsValueOptional<T>(this IArg<T, OptionArgInfo> arg, bool value) =>
            arg.WithInfo(arg.Info.WithIsValueOptional(value));

        static IArg<T, TInfo>
            Create<T, TInfo>(TInfo data, Func<IAccumulator<T>> accumulatorFactory,
                         Func<IAccumulator<T>, T> binder) where TInfo : IArgInfo =>
            new Arg<T, TInfo>(data, accumulatorFactory, binder);

        public static IArg<bool, OptionArgInfo> Flag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Flag(ShortOptionName.From(s[0])),
                _ => Flag(name, null)
            };

        public static IArg<bool, OptionArgInfo> Flag(char shortName) =>
            Flag(ShortOptionName.From(shortName));

        public static IArg<bool, OptionArgInfo> Flag(ShortOptionName shortName) =>
            Flag(null, shortName);

        public static IArg<bool, OptionArgInfo> Flag(string name, ShortOptionName shortName) =>
            Create(new OptionArgInfo(OptionArgKind.Flag, name, shortName),
                   () => Accumulator.Create(false, (_, r) => ParseResult.Success(true)),
                   r => r.Count > 0);

        public static IArg<int, OptionArgInfo> CountedFlag(string name) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => CountedFlag(ShortOptionName.From(s[0])),
                _ => CountedFlag(name, null)
            };

        public static IArg<int, OptionArgInfo> CountedFlag(char shortName) =>
            CountedFlag(ShortOptionName.From(shortName));

        public static IArg<int, OptionArgInfo> CountedFlag(ShortOptionName shortName) =>
            CountedFlag(null, shortName);

        public static IArg<int, OptionArgInfo> CountedFlag(string name, ShortOptionName shortName) =>
            Create(new OptionArgInfo(OptionArgKind.Flag, name, shortName),
                   () => Accumulator.Create(0, (count, _) => ParseResult.Success(count + 1)),
                   r => r.GetResult());

        public static IArg<T, OptionArgInfo> Option<T>(string name, T @default, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), @default, parser),
                _ => Option(name, null, @default, parser)
            };

        public static IArg<T, OptionArgInfo> Option<T>(char shortName, T @default, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), @default, parser);

        public static IArg<T, OptionArgInfo> Option<T>(ShortOptionName shortName, T @default, IParser<T> parser) =>
            Option(null, shortName, @default, parser);

        public static IArg<T, OptionArgInfo> Option<T>(string name, ShortOptionName shortName, T @default, IParser<T> parser) =>
            Create(new OptionArgInfo(name, shortName), () => Accumulator.Value(parser, default, @default, (_, v) => v), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<T, OptionArgInfo> Option<T>(string name, IParser<T> parser) =>
            name switch
            {
                null => throw new ArgumentNullException(nameof(name)),
                var s when s.Length == 0 => throw new ArgumentException(null, nameof(name)),
                var s when s.Length == 1 => Option(ShortOptionName.From(s[0]), parser),
                _ => Option(name, null, parser)
            };

        public static IArg<T, OptionArgInfo> Option<T>(char shortName, IParser<T> parser) =>
            Option(ShortOptionName.From(shortName), parser);

        public static IArg<T, OptionArgInfo> Option<T>(ShortOptionName shortName, IParser<T> parser) =>
            Option(null, shortName, parser);

        public static IArg<T, OptionArgInfo> Option<T>(string name, ShortOptionName shortName, IParser<T> parser) =>
            Option(name, shortName, default, parser);

        public static IArg<int, IntegerOptionArgInfo> IntOpt(string name) =>
            IntOpt(name, -1);

        public static IArg<int, IntegerOptionArgInfo> IntOpt(string name, int @default) =>
            IntOpt(name, @default, Parser.Int32());

        public static IArg<T, IntegerOptionArgInfo> IntOpt<T>(string name, IParser<T> parser) =>
            IntOpt(name, default, parser);

        public static IArg<T, IntegerOptionArgInfo> IntOpt<T>(string name, T @default, IParser<T> parser) =>
            Create(new IntegerOptionArgInfo(name), () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<T, OperandArgInfo> Operand<T>(string name, IParser<T> parser) =>
            Operand(name, default, parser);

        public static IArg<T, OperandArgInfo> Operand<T>(string name, T @default, IParser<T> parser) =>
            Create(new OperandArgInfo(name), () => Accumulator.Value(parser), r => r.Count > 0 ? r.GetResult() : @default);

        public static IArg<string, LiteralArgInfo> Literal(string value) =>
            Literal(value, StringComparison.Ordinal);

        public static IArg<string, LiteralArgInfo> Literal(string value, StringComparison comparison)
        {
            var parser = Parser.Literal(value, comparison);
            return Create(new LiteralArgInfo(value), () => Accumulator.Value(parser), r => r.GetResult());
        }

        public static IArg<ImmutableArray<T>, OperandArgInfo> List<T>(this IArg<T, OperandArgInfo> arg) =>
            List<T, OperandArgInfo>(arg);

        public static IArg<ImmutableArray<T>, OptionArgInfo> List<T>(this IArg<T, OptionArgInfo> arg) =>
            List<T, OptionArgInfo>(arg);

        public static IArg<(bool Present, T Value), TInfo>
            FlagPresence<T, TInfo>(this IArg<T, TInfo> arg) where TInfo : IArgInfo =>
            arg.FlagPresence(false, true);

        public static IArg<(P Presence, T Value), TInfo>
            FlagPresence<T, P, TInfo>(this IArg<T, TInfo> arg, P absent, P present)
            where TInfo : IArgInfo =>
            Create(arg.Info,
                   () => from v in arg.CreateAccumulator()
                         select (Presence: present, Value: v),
                   r => r.Count > 0 ? (present, arg.Bind(() => Accumulator.Return(r.GetResult().Item2))) : (absent, default));

        static IArg<ImmutableArray<T>, TInfo> List<T, TInfo>(IArg<T, TInfo> arg)
            where TInfo : IArgInfo =>
            Create(arg.Info,
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
                           array =>
                           {
                               var accumulator = arg.CreateAccumulator();
                               accumulator.ReadDefault();
                               array.Add(arg.Bind(() => accumulator));
                               return array;
                           },
                           a => a.ToImmutable()),
                       r => r.Count > 0 ? r.GetResult() : ImmutableArray<T>.Empty);

        public static IArg<ImmutableArray<T>, TInfo> Tail<T, TInfo>(this IArg<T, TInfo> arg)
            where TInfo : IArgInfo =>
            Create(arg.Info,
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
