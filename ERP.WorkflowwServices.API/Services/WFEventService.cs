using ERP.WorkflowwServices.API.DTOs.FilterModels;
using ERP.WorkflowwServices.API.Interfaces;
using ERP.WorkflowwServices.API.Models;
using ERP.WorkflowwServices.API.Services.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ERP.WorkflowwServices.API.Services
{
    public class WFEventService : IWFEvent
    {
        IConfiguration _configuration;
        private bool disposedValue;
        private readonly WorkflowDbContext _context;
        private IHttpContextAccessor _httpContextAccessor;
        public WFEventService(WorkflowDbContext context, IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _configuration = configuration; _httpContextAccessor = httpContextAccessor;
        }

        public async Task<int> AddWFEvent(WFEvent item)
        {
            try
            {
                if (item.EventId == 0)
                {
                    var exists = await _context.WFEvents.AnyAsync(x => x.TableName == item.TableName);
                    if (exists)
                        return -1;

                    await _context.WFEvents.AddAsync(item);
                    await _context.SaveChangesAsync();
                    return item.EventId;
                }
                /* UPDATE */
                var existing = await _context.WFEvents.FirstOrDefaultAsync(x => x.EventId == item.EventId);
                if (existing == null)
                    return -2;

                existing.Description = item.Description;
                existing.TableName = item.TableName;
                existing.PageId = item.PageId;
                existing.CompanyId = item.CompanyId;
                existing.MultiUse = item.MultiUse;
                existing.CheckShow = item.CheckShow;
                existing.ModifiedBy = item.ModifiedBy;
                existing.ModifiedDate = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return existing.EventId;
            }
            catch (Exception)
            {
                return -10;
            }
        }

        public async Task<int> ToggleDeleteWFEventsAsync(List<int> eventIds, int userId)
        {
            try
            {
                var events = await _context.WFEvents.Where(x => eventIds.Contains(x.EventId)).ToListAsync();
                foreach (var item in events)
                {
                    if (item.IsDeleted == false)
                        item.IsDeleted = true;
                    else
                        item.IsDeleted = false;
                    item.ModifiedBy = userId;
                    item.ModifiedDate = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return events.Count;
            }
            catch (Exception)
            {
                return -10;
            }
        }

        public async Task<(IEnumerable<WFEvent> Data, int Total)> FilterWFEventAsync(WorkFlowFilterModel paging, string? keyword)
        {
            bool checkStatus = paging.Status == 1;
            var query = _context.WFEvents.AsQueryable();
            query = query.Where(t => t.IsDeleted == checkStatus);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                query = query.Where(t => !string.IsNullOrEmpty(t.TableName) && t.TableName.Contains(keyword));
            }

            /* Company filter */
            if (paging.CompanyId.HasValue)
            {
                query = query.Where(t => t.CompanyId == paging.CompanyId);
            }

            /* Page filter */
            if (paging.PageId.HasValue)
            {
                query = query.Where(t => t.PageId == paging.PageId);
            }

            int total = await query.CountAsync();
            var data = await query
                .OrderBy(c => c.TableName)
                .Skip((paging.PageNumber - 1) * paging.PageSize)
                .Take(paging.PageSize)
                .ToListAsync();

            return (data, total);
        }

        public async Task<IEnumerable<WFEvent>> GetWFEventAsync(int status)
        {
            bool? checkStatus = null;
            if (status <= 1)
                checkStatus = status == 1;

            var query = _context.WFEvents.AsQueryable();
            if (checkStatus != null)
            {
                query = query.Where(t => t.IsDeleted == checkStatus);
            }

            return await query.ToListAsync();
        }

        public async Task<IEnumerable<WFEvent>> GetEventByCompanyAsync(int status, int companyId)
        {
            bool? checkStatus = null;

            if (status <= 1)
                checkStatus = status == 1;

            var query = _context.WFEvents.Where(t => t.CompanyId == companyId);

            if (checkStatus != null)
            {
                query = query.Where(t => t.IsDeleted == checkStatus);
            }

            return await query.ToListAsync();
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                disposedValue = true;
            }
        }
    }
}
