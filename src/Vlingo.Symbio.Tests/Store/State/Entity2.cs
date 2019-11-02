// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

namespace Vlingo.Symbio.Tests.Store.State
{
    public class Entity2
    {
        public string Id { get; }
        
        public int Value { get; }

        public Entity2(string id, int value)
        {
            Id = id;
            Value = value;
        }
    }
}