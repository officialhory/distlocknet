﻿using DistLockNet.Interfaces;
using DistLockNet.Models;
using System;
using System.Threading.Tasks;

namespace DistLockNetPGBnd
{
    public class PgBnd: ILockingBnd
    {
        public async Task<LockingObject> GetAsync(string application)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> AddAsync(string application, Guid lockerId, Guid seed)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> UpdateAsync(string application, Guid lockerId, Guid seed)
        {
            throw new NotImplementedException();
        }
    }
}
