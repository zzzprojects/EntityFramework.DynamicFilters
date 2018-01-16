using System;
using System.Data.Entity;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace DelegateBenchmark
{
    public class DelegateInvoking
    {
        private readonly MulticastDelegate _multicastDelegate;

        private readonly Func<DbContext, bool> _funcDelegate;

        private readonly DbContext _dbContext;

        public DelegateInvoking()
        {
            Func<bool> condition = () => { return false; };

            this._multicastDelegate = (MulticastDelegate) condition;
            this._funcDelegate = (DbContext dbcontext) => { return condition(); };
            this._dbContext = new DbContext("default");
        }

        [Benchmark]
        public bool MulticastDelegate_DynamicInvoke()
        {
            var func = this._multicastDelegate;
            var parameters = func.Method.GetParameters();
            if (parameters.Any())
                return (bool) func.DynamicInvoke(_dbContext);

            return (bool) func.DynamicInvoke();
        }

        [Benchmark]
        public bool FunDelegate_Invoke()
        {
            return this._funcDelegate.Invoke(this._dbContext);
        }
    }
}