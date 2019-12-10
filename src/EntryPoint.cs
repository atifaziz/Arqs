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

namespace Arqs
{
    using System;
    using System.Collections.Immutable;
    using System.Threading.Tasks;

    public enum EntryPointMode { RunMain, ShowHelp }

    public interface IEntryPoint
    {
        EntryPointMode Mode { get; }
        Task<int> Main(ImmutableArray<string> args);
    }

    sealed class EntryPoint : IEntryPoint
    {
        readonly Func<ImmutableArray<string>, Task<int>> _main;

        public EntryPoint(EntryPointMode mode, Func<ImmutableArray<string>, Task<int>> main)
        {
            Mode = mode;
            _main = main ?? throw new ArgumentNullException(nameof(main));
        }

        public EntryPointMode Mode { get; }

        public Task<int> Main(ImmutableArray<string> args) =>
            _main(args);
    }
}
