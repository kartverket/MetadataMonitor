using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using Npgsql;
using NpgsqlTypes;

namespace Kartverket.MetadataMonitor.Models
{
    public class MetadataRepository
    {
        private readonly string _connectionString;

        public MetadataRepository()
        {
            _connectionString = ConfigurationManager.ConnectionStrings["MonitorDatabaseContext"].ConnectionString;
        }

        private NpgsqlConnection GetConnection()
        {
            return new NpgsqlConnection(_connectionString);
        }


        public List<MetadataEntry> GetMetadataList(string organization, string resourceType, bool? inspireResource)
        {
            var metadataEntries = new List<MetadataEntry>();

            NpgsqlConnection connection = GetConnection();
            connection.Open();
            try
            {
                string sql = "SELECT m.uuid, m.title, m.responsible_organization, m.resourcetype, m.inspire_resource, m.keywords, m.contact_information, m.abstract, m.purpose FROM metadata m " 
                    + CreateMetaConditionsSql(organization, resourceType, inspireResource);

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    AddMetadataConditionParametersToCommand(null, organization, resourceType, inspireResource, command);

                    using (NpgsqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var metadataEntry = new MetadataEntry()
                            {
                                InspireResource = dr.GetBoolean(4),
                                ResourceType = dr.GetString(3),
                                ResponsibleOrganization = dr.GetString(2),
                                Title = dr.GetString(1),
                                Uuid = dr.GetString(0),
                                Keywords = dr.IsDBNull(5) ? null : dr.GetString(5),
                                ContactInformation = dr.IsDBNull(6) ? null : dr.GetString(6),
                                Abstract = dr.IsDBNull(7) ? null : dr.GetString(7),
                                Purpose = dr.IsDBNull(8) ? null : dr.GetString(8),
                            };
                            metadataEntries.Add(metadataEntry);
                        }
                    }
                }
            }
            finally
            {
                connection.Close();
            }

            return metadataEntries;
        }

        private static void AddMetadataConditionParametersToCommand(int? status, string organization, string resourceType, bool? inspireResource, NpgsqlCommand command)
        {
            if (status.HasValue)
                command.Parameters.Add(new NpgsqlParameter("status", NpgsqlDbType.Integer) { Value = status });
            if (!string.IsNullOrWhiteSpace(organization))
                command.Parameters.Add(new NpgsqlParameter("responsible_organization", NpgsqlDbType.Varchar) {Value = organization});
            if (!string.IsNullOrWhiteSpace(resourceType))
                command.Parameters.Add(new NpgsqlParameter("resourcetype", NpgsqlDbType.Varchar) {Value = resourceType});
            if (inspireResource.HasValue)
                command.Parameters.Add(new NpgsqlParameter("inspire_resource", NpgsqlDbType.Boolean) {Value = inspireResource});
        }

        public List<MetadataEntry> GetMetadataListWithLatestValidationResult(int? status, string organization, string resourceType, bool? inspireResource)
        {
            var metadataEntries = new List<MetadataEntry>();

            NpgsqlConnection connection = GetConnection();
            connection.Open();
            try
            {
                var sql = SelectWhichSqlToUse(status, organization, resourceType, inspireResource);

                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    AddMetadataConditionParametersToCommand(status, organization, resourceType, inspireResource, command);

                    using (NpgsqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var metadataEntry = new MetadataEntry()
                                {
                                    InspireResource = dr.GetBoolean(4),
                                    ResourceType = dr.GetString(3),
                                    ResponsibleOrganization = dr.GetString(2),
                                    Title = dr.GetString(1),
                                    Uuid = dr.GetString(0),
                                    ValidationResults = new List<ValidationResult>()
                                        {
                                            new ValidationResult()
                                                {
                                                    Messages = dr.IsDBNull(6) ? null : dr.GetString(6),
                                                    Result = dr.GetInt32(5),
                                                    Timestamp = dr.GetTimeStamp(7)
                                                }
                                        }
                                };
                            metadataEntries.Add(metadataEntry);
                        }
                    }
                }
            }
            finally
            {
                connection.Close();
            }
            return metadataEntries;
        }

        private string SelectWhichSqlToUse(int? status, string organization, string resourceType, bool? inspireResource)
        {
            string sql = "SELECT m.uuid, m.title, m.responsible_organization, m.resourcetype, m.inspire_resource, " +
                "subQuery.result, subQuery.messages, subQuery.timestamp FROM metadata m " +
                "INNER JOIN " +
                    "(SELECT res.uuid, res.result, res.messages, res.timestamp " +
                    "FROM validation_results res " +
                    "WHERE (res.uuid, res.timestamp) IN " +
                        "(SELECT v.uuid, MAX(timestamp) as timestamp " +
                        "FROM validation_results v " +
                        "GROUP BY v.uuid) " +
                    " __RESULT_CONDITIONS__ " +
                    ") AS subQuery " +
                "ON subQuery.uuid = m.uuid " +
                " __META_CONDITIONS__ " +
                "ORDER BY subQuery.timestamp desc";
            
            sql = sql.Replace("__RESULT_CONDITIONS__", CreateResultConditionsSql(status));

            sql = sql.Replace("__META_CONDITIONS__", CreateMetaConditionsSql(organization, resourceType, inspireResource));

            return sql;
        }

        private static string CreateResultConditionsSql(int? status)
        {
            var sqlResultConditions = "";

            if (status.HasValue)
            {
                sqlResultConditions = "AND res.result = :status";
            }
            return sqlResultConditions;
        }

        private static string CreateMetaConditionsSql(string organization, string resourceType, bool? inspireResource)
        {
            List<string> metaConditions = new List<string>();
            metaConditions.Add(" m.active = true ");
            if (!string.IsNullOrWhiteSpace(organization))
                metaConditions.Add(" m.responsible_organization LIKE :responsible_organization ");

            if (!string.IsNullOrWhiteSpace(resourceType))
                metaConditions.Add(" m.resourcetype = :resourcetype ");

            if (inspireResource.HasValue)
                metaConditions.Add(" m.inspire_resource = :inspire_resource ");

            StringBuilder metaConditionsSql = new StringBuilder();
            for (int i = 0; i < metaConditions.Count; i++)
            {
                metaConditionsSql.Append(i == 0 ? " WHERE " : " AND ");
                metaConditionsSql.Append(metaConditions[i]);
            }

            return metaConditionsSql.ToString();
        }

        public void SaveMetadata(MetadataEntry metadata)
        {
            NpgsqlConnection connection = GetConnection();
            connection.Open();
            NpgsqlTransaction transaction = connection.BeginTransaction();
            try
            {
                if (MetadataExists(metadata.Uuid, connection))
                {
                    UpdateMetadataInformation(metadata, connection);
                }
                else
                {
                    InsertMetadataInformation(metadata, connection);
                }

                InsertMetadataValidationResult(metadata, connection);

                transaction.Commit();
            }
            catch (NpgsqlException e)
            {
                throw e;
            }
            finally
            {
                connection.Close();
            }
        }

        private void InsertMetadataValidationResult(MetadataEntry metadata, NpgsqlConnection connection)
        {
            var validationResult = metadata.ValidationResults[0];

            const string sql =
                "INSERT INTO validation_results (uuid, result, messages) VALUES (:uuid, :result, :messages)";
            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("uuid", NpgsqlDbType.Varchar) {Value = metadata.Uuid});
                command.Parameters.Add(new NpgsqlParameter("result", NpgsqlDbType.Integer)
                    {
                        Value = validationResult.Result
                    });
                command.Parameters.Add(new NpgsqlParameter("messages", NpgsqlDbType.Varchar)
                    {
                        Value = validationResult.Messages
                    });
                command.ExecuteNonQuery();
            }
        }

        private void UpdateMetadataInformation(MetadataEntry metadata, NpgsqlConnection connection)
        {
            const string sql = "UPDATE metadata SET " +
                               "title = :title, " +
                               "responsible_organization = :responsible_organization, " +
                               "resourcetype = :resourcetype, " +
                               "inspire_resource = :inspire_resource, " +
                               "active = true, " +
                               "keywords = :keywords, " +
                               "contact_information = :contact_information, " +
                               "abstract = :abstract, " +
                               "purpose = :purpose " +
                               "WHERE uuid = :uuid";
            RunInsertUpdateMetadataCommand(metadata, sql, connection);
        }

        private void InsertMetadataInformation(MetadataEntry metadata, NpgsqlConnection connection)
        {
            const string sql =
                "INSERT INTO metadata (uuid, title, responsible_organization, resourcetype, inspire_resource, active, keywords, contact_information, abstract, purpose) VALUES " +
                "(:uuid, :title, :responsible_organization, :resourcetype, :inspire_resource, true, :keywords, :contact_information, :abstract, :purpose)";
            RunInsertUpdateMetadataCommand(metadata, sql, connection);
        }

        private static void RunInsertUpdateMetadataCommand(MetadataEntry metadata, string sql,
                                                           NpgsqlConnection connection)
        {
            using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
            {
                command.Parameters.Add(new NpgsqlParameter("uuid", NpgsqlDbType.Varchar) {Value = metadata.Uuid});
                command.Parameters.Add(new NpgsqlParameter("title", NpgsqlDbType.Varchar) {Value = metadata.Title});
                command.Parameters.Add(new NpgsqlParameter("responsible_organization", NpgsqlDbType.Varchar)
                    {
                        Value = metadata.ResponsibleOrganization
                    });
                command.Parameters.Add(new NpgsqlParameter("resourcetype", NpgsqlDbType.Varchar)
                    {
                        Value = metadata.ResourceType
                    });
                command.Parameters.Add(new NpgsqlParameter("inspire_resource", NpgsqlDbType.Boolean)
                    {
                        Value = metadata.InspireResource
                    });
                command.Parameters.Add(new NpgsqlParameter("keywords", NpgsqlDbType.Varchar) { Value = metadata.Keywords });
                command.Parameters.Add(new NpgsqlParameter("contact_information", NpgsqlDbType.Varchar) { Value = metadata.ContactInformation });
                command.Parameters.Add(new NpgsqlParameter("abstract", NpgsqlDbType.Varchar) { Value = metadata.Abstract });
                command.Parameters.Add(new NpgsqlParameter("purpose", NpgsqlDbType.Varchar) { Value = metadata.Purpose });
                command.ExecuteNonQuery();
            }
        }

        private bool MetadataExists(string uuid, NpgsqlConnection connection)
        {
            bool result;
            using (
                NpgsqlCommand command = new NpgsqlCommand("SELECT count(uuid) FROM metadata WHERE uuid = :uuid",
                                                          connection))
            {
                command.Parameters.Add(new NpgsqlParameter("uuid", NpgsqlDbType.Varchar) {Value = uuid});
                var count = (System.Int64) command.ExecuteScalar();
                result = count != 0;
            }
            return result;
        }

        public IEnumerable<string> GetAvailableOrganizations()
        {
            const string sql = "SELECT DISTINCT responsible_organization FROM metadata ORDER BY responsible_organization ASC";

            List<string> organizations = new List<string>();

            NpgsqlConnection connection = GetConnection();
            connection.Open();
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    using (NpgsqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            organizations.Add(dr.GetString(0));
                        }
                    }
                }
            }
            finally
            {
                connection.Close();
            }

            return organizations;
        }

        public void DeactivateAllMetadata()
        {
            const string sql = "UPDATE metadata SET active = false";
            NpgsqlConnection connection = GetConnection();
            connection.Open();
            try
            {
                using (NpgsqlCommand command = new NpgsqlCommand(sql, connection))
                {
                    command.ExecuteNonQuery();
                }
            }
            finally
            {
                connection.Close();
            }

        }
    }
}