﻿// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Vlingo.Actors.TestKit;
using Vlingo.Common;
using Vlingo.Symbio.Store;
using Vlingo.Symbio.Store.Dispatch;
using Vlingo.Symbio.Store.State;

namespace Vlingo.Symbio.Tests.Store.State
{
    public class MockStateStoreResultInterest : IReadResultInterest, IWriteResultInterest, IConfirmDispatchedResultInterest
    {
        private AccessSafely _access;

        public AtomicInteger _confirmDispatchedResultedIn = new AtomicInteger(0);
        public AtomicInteger _readObjectResultedIn = new AtomicInteger(0);
        public AtomicInteger _writeObjectResultedIn = new AtomicInteger(0);

        public AtomicRefValue<Result> _objectReadResult = new AtomicRefValue<Result>();
        public AtomicRefValue<Result> _objectWriteResult = new AtomicRefValue<Result>();
        public ConcurrentQueue<Result> _objectWriteAccumulatedResults = new ConcurrentQueue<Result>();
        public AtomicReference<Metadata> _metadataHolder = new AtomicReference<Metadata>();
        public AtomicReference<object> _objectState = new AtomicReference<object>();
        public ConcurrentQueue<Exception> _errorCauses = new ConcurrentQueue<Exception>();
        public ConcurrentQueue<object> _sources = new ConcurrentQueue<object>();

        public MockStateStoreResultInterest()
        {
            _access = AfterCompleting<object, object>(0);
        }
        
        public void ReadResultedIn<TState>(IOutcome<StorageException, Result> outcome, string id, TState state, int stateVersion, Metadata metadata, object @object)
        {
            outcome
                .AndThen(result => {
                    _access.WriteUsing("readStoreData", new StoreData<TState>(1, result, state, new List<Source<TState>>(), metadata, null));
                    return result; 
                })
                .Otherwise(cause => {
                    _access.WriteUsing("readStoreData", new StoreData<TState>(1, cause.Result, state, new List<Source<TState>>(), metadata, cause));
                    return cause.Result;
                });
        }

        public void WriteResultedIn<TState, TSource>(IOutcome<StorageException, Result> outcome, string id, TState state, int stateVersion, IEnumerable<Source<TSource>> sources, object @object)
        {
            outcome
                .AndThen(result => {
                    _access.WriteUsing("writeStoreData", new StoreData<TSource>(1, result, state, sources, null, null));
                    return result;
                })
                .Otherwise(cause => {
                    _access.WriteUsing("writeStoreData", new StoreData<TSource>(1, cause.Result, state, sources, null, cause));
                    return cause.Result;
                });
        }

        public void ConfirmDispatchedResultedIn(Result result, string dispatchId)
        {
            // not used
        }
        
        public AccessSafely AfterCompleting<TState, TSource>(int times)
        {
            _access = AccessSafely.AfterCompleting(times);
                
            _access
                .WritingWith<int>("confirmDispatchedResultedIn", increment => _confirmDispatchedResultedIn.AddAndGet(increment))
                .ReadingWith("confirmDispatchedResultedIn", () => _confirmDispatchedResultedIn.Get())

                .WritingWith<StoreData<TSource>>("writeStoreData", data =>
                    {
                        _writeObjectResultedIn.AddAndGet(data.ResultedIn);
                        _objectWriteResult.Set(data.Result);
                        _objectWriteAccumulatedResults.Enqueue(data.Result);
                        _objectState.Set(data.State);
                        data.Sources.ForEach(source => _sources.Enqueue(source));
                        _metadataHolder.Set(data.Metadata);
                        if (data.ErrorCauses != null)
                        {
                            _errorCauses.Enqueue(data.ErrorCauses);
                        }
                    })
                .WritingWith<StoreData<TSource>>("readStoreData", data =>
                {
                    _readObjectResultedIn.AddAndGet(data.ResultedIn);
                    _objectReadResult.Set(data.Result);
                    _objectWriteAccumulatedResults.Enqueue(data.Result);
                    _objectState.Set(data.State);
                    data.Sources.ForEach(source => _sources.Enqueue(source));
                    _metadataHolder.Set(data.Metadata);
                    if (data.ErrorCauses != null)
                    {
                        _errorCauses.Enqueue(data.ErrorCauses);
                    }
                })
                .ReadingWith("readObjectResultedIn", () => _readObjectResultedIn.Get())
                .ReadingWith("objectReadResult", () => _objectReadResult.Get())
                .ReadingWith("objectWriteResult", () => _objectWriteResult.Get())
                .ReadingWith("objectWriteAccumulatedResults", () =>
                {
                    _objectWriteAccumulatedResults.TryDequeue(out var result);
                    return result;
                })
                .ReadingWith("objectWriteAccumulatedResultsCount", () => _objectWriteAccumulatedResults.Count)
                .ReadingWith("metadataHolder", () => _metadataHolder.Get())
                .ReadingWith("objectState", () => (TState) _objectState.Get())
                .ReadingWith("sources", () =>
                {
                    _sources.TryDequeue(out var result);
                    return result;
                })
                .ReadingWith("errorCauses", () =>
                {
                    _errorCauses.TryDequeue(out var result);
                    return result;
                })
                .ReadingWith("errorCausesCount", () => _errorCauses.Count)
                .ReadingWith("writeObjectResultedIn", () => _writeObjectResultedIn.Get());

            return _access;
        }
    }
    
    public class StoreData<TSource> {
        public Exception ErrorCauses { get; }
        public Metadata Metadata { get; }
        public Result Result { get; }
        public List<Source<TSource>> Sources { get; }
        public object State { get; }
        public int ResultedIn { get; }

        public StoreData(int resultedIn, Result objectResult, object state, IEnumerable<Source<TSource>> sources, Metadata metadata, Exception errorCauses) {
            ResultedIn = resultedIn;
            Result = objectResult;
            State = state;
            Sources = new List<Source<TSource>>(sources);
            Metadata = metadata;
            ErrorCauses = errorCauses;
        }
    }
}