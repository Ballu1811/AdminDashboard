using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Repositories.Interfaces;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.WorkflowwServices.API.Services
{
    public class WFEventService : IWFEvent
    {
        private readonly IRepository<WFEvent, Guid> _repository;
        public WFEventService(IRepository<WFEvent, Guid> repository)
        {
            _repository = repository;
        }

        public async Task<int> AddWFEvent(WFEvent item)
        {
            await _repository.AddAsync(item);
            await _repository.SaveChangesAsync();
            return item.EventId;

            #region Not Use            
            //try
            //{
            //    if (item.EventId == 0)
            //    {
            //        var exists = await _context.WFEvents.AnyAsync(x => x.TableName == item.TableName);
            //        if (exists)
            //            return -1;

            //        await _context.WFEvents.AddAsync(item);
            //        await _context.SaveChangesAsync();
            //        return item.EventId;
            //    }
            //    /* UPDATE */
            //    var existing = await _context.WFEvents.FirstOrDefaultAsync(x => x.EventId == item.EventId);
            //    if (existing == null)
            //        return -2;

            //    existing.Description = item.Description;
            //    existing.TableName = item.TableName;
            //    existing.PageId = item.PageId;
            //    existing.CompanyId = item.CompanyId;
            //    existing.MultiUse = item.MultiUse;
            //    existing.CheckShow = item.CheckShow;
            //    existing.ModifiedBy = item.ModifiedBy;
            //    existing.ModifiedDate = DateTime.UtcNow;
            //    await _context.SaveChangesAsync();

            //    return existing.EventId;
            //}
            //catch (Exception)
            //{
            //    return -10;
            //}
            #endregion
        }

        public async Task<int> ToggleDeleteWFEventsAsync(List<int> eventIds, int userId)
        {
            var events = await _repository.FindAsync(x => eventIds.Contains(x.EventId));
            foreach (var item in events)
            {
                item.IsDeleted = !item.IsDeleted;
                _repository.Update(item);
            }

            await _repository.SaveChangesAsync();

            return events.Count();
        }

        public async Task<(IEnumerable<WFEvent> Data, int Total)> FilterWFEventAsync(WorkFlowFilterModel paging, string? keyword)
        {
            var query = _repository.Query();

            if (!string.IsNullOrWhiteSpace(keyword))
                query = query.Where(x => x.Name.Contains(keyword));

            int total = await query.CountAsync();

            var data = await query.OrderBy(x => x.Name).Skip((paging.PageNumber - 1) * paging.PageSize).Take(paging.PageSize)
                   .ToListAsync();

            return (data, total);
        }

        public async Task<IEnumerable<WFEvent>> GetWFEventAsync(int status)
        {
            var query = _repository.Query();

            if (status <= 1)
                query = query.Where(x => x.IsDeleted == (status == 1));

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<WFEvent>> GetEventByCompanyAsync(int status, Guid tenantId)
        {
            var query = _repository.Query().Where(x => x.TenantId == tenantId);

            if (status == 0)
                query = query.Where(x => !x.IsDeleted);

            else if (status == 1)
                query = query.Where(x => x.IsDeleted);

            return await query.ToListAsync();
        }

        public async Task<WFEvent?> GetWFEventIdAsync(int id)
        {
            return await _repository.Query().AsNoTracking().FirstOrDefaultAsync(x => x.EventId == id);
        }
    }
}
