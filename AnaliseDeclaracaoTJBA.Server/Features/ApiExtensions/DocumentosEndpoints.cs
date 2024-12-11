using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;

namespace AnaliseDeclaracaoTJBA.Server.Features.ApiExtensions;

public static class DocumentosEndpoints
{
    public static void MapEndpointsDocumentos(this WebApplication app)
    {
        _ = app.MapGet("/api/documentos-list", async (IMongoClient client, int page = 1, int pageSize = 200, string? status = null, string? cpfCnpj = null, string? certidaoNumero = null, bool validado = false) =>
        {
            try
            {
                var database = client.GetDatabase("AnaliseTJBA");
                var collection = database.GetCollection<BsonDocument>("Documentos");


                var filter = Builders<BsonDocument>.Filter.Empty;
                if (!string.IsNullOrEmpty(cpfCnpj))
                {
                    filter = Builders<BsonDocument>.Filter.Eq("cpfCnpj", cpfCnpj);
                }
                if (!string.IsNullOrEmpty(certidaoNumero))
                {
                    filter = Builders<BsonDocument>.Filter.Eq("certidaoNumero", certidaoNumero);
                }
                if (!string.IsNullOrEmpty(status))
                {
                    filter = Builders<BsonDocument>.Filter.Eq("statusProcessamentoCertidao", status);
                }

                var totalDocuments = await collection.CountDocumentsAsync(filter);
                var totalPages = (int)Math.Ceiling((double)totalDocuments / pageSize);

                var documentos = await collection.Find(filter)
                    .Sort(Builders<BsonDocument>.Sort.Descending("dataCertidao"))
                    .Skip((page - 1) * pageSize)
                    .Limit(pageSize)
                    .ToListAsync();

                if (!documentos.Any())
                {
                    return Results.NotFound("Nenhum documento encontrado.");
                }
                var result = new
                {
                    totalPages,
                    currentPage = page,
                    pageSize,
                    totalDocuments,
                    data = documentos.Select(d => new
                    {
                        _id = d["_id"].ToString(),
                        cpfCnpj = d["cpfCnpj"].AsString,
                        razaoSocial = d["razaoSocial"].AsString,
                        certidaoNumero = d["certidaoNumero"].AsString,
                        dataPrazoCertidao = d.Contains("dataPrazoCertidao") && d["dataPrazoCertidao"].IsValidDateTime
                                            ? d["dataPrazoCertidao"].ToUniversalTime()
                                            : DateTime.MinValue,
                        StatusProcessamentoCertidao = d["statusProcessamentoCertidao"].AsString,
                        validado = d["validado"].AsBoolean,
                        logs = d["logs"].AsBsonArray.Select(log => new
                        {
                            acao = log["acao"].ToString(),
                            data = log["data"].ToUniversalTime(),
                            usuario = log["usuario"].ToString(),
                        }).ToList(),
                        possuiArquivoPdf = d.Contains("fileId") && ObjectId.TryParse(d["fileId"].ToString(), out _)
                    })
                };

                return Results.Ok(new { data = result });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar documentos: {ex.Message}");
                return Results.Problem("Erro ao carregar documentos.");
            }
        });

        _ = app.MapGet("/api/documentos-list/{id}/download", async (IMongoClient client, string id) =>
       {
           try
           {
               if (string.IsNullOrEmpty(id) || !ObjectId.TryParse(id, out var objectId))
               {
                   return Results.BadRequest("ID do arquivo inválido.");
               }
               var database = client.GetDatabase("AnaliseTJBA");
               var documentosCollection = database.GetCollection<BsonDocument>("Documentos");
               var documentoFilter = Builders<BsonDocument>.Filter.Eq("_id", objectId);
               var documento = await documentosCollection.Find(documentoFilter).FirstOrDefaultAsync();


               if (documento == null)
               {
                   return Results.NotFound("Documento não encontrado.");
               }

               // Obtém o fileId do documento para buscar o arquivo no GridFS
               if (!documento.Contains("fileId") || !ObjectId.TryParse(documento["fileId"].ToString(), out var fileObjectId))
               {
                   return Results.NotFound("Arquivo associado ao documento não encontrado.");
               }
               var gridFS = new GridFSBucket(database);
               var fileFilter = Builders<GridFSFileInfo>.Filter.Eq("_id", fileObjectId);
               var fileInfo = await gridFS.Find(fileFilter).FirstOrDefaultAsync();

               if (fileInfo == null)
               {
                   return Results.NotFound("Arquivo físico não encontrado no GridFS.");
               }
               using var memoryStream = new MemoryStream();
               await gridFS.DownloadToStreamAsync(fileObjectId, memoryStream);

               memoryStream.Position = 0;
               // Retorna o arquivo com o nome original
               return Results.File(memoryStream.ToArray(), "application/pdf", fileInfo.Filename);

           }
           catch (Exception ex)
           {
               Console.WriteLine($"Erro ao baixar o documento: {ex.Message}");
               return Results.Problem("Erro ao baixar o documento.");
           }
       });
    }
}