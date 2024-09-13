using Azure.Storage.Blobs.Models;
using Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mystiqu.eventgrid.dlqmanager.domain;

namespace mystiqu.eventgrid.dlqmanager
{
    public interface IDLQManager
    {
        List<string> ListDeadletterDateFolders(string _baseFolder, int maxDepth);
        IEnumerable<Page<BlobItem>> ListBlobs(string prefix, int pageSize);

        List<DownloadedBlob> DownloadBlobs(Page<BlobItem> blobPage, int page, int pageSize, string[] invalidFilers);
        List<EventGridDeadLetterEvent> PublishEvents(List<DownloadedBlob> blobs, bool deleteBlobs = false);

        List<string> DeleteBlobs(List<DownloadedBlob> downloadedBlobs);
    }
}
