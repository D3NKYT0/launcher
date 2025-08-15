using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Updater.Models
{
    /// <summary>
    /// Gerenciador de configuração que permite sobrescrever configurações padrão com arquivo opcional
    /// </summary>
    public static class ConfigurationManager
    {
        private const string CONFIG_FILE_NAME = "launcher_config.json";
        
        /// <summary>
        /// Carrega as configurações, priorizando arquivo externo se existir
        /// </summary>
        /// <param name="logger">Logger para registrar operações</param>
        /// <returns>Configurações carregadas</returns>
        public static AppSettings LoadSettings(ILogger? logger = null)
        {
            try
            {
                // Primeiro, tenta carregar configurações do arquivo externo
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);
                
                if (File.Exists(configPath))
                {
                    logger?.LogInformation("Carregando configurações do arquivo: {ConfigPath}", configPath);
                    
                    var jsonContent = File.ReadAllText(configPath);
                    var externalSettings = JsonSerializer.Deserialize<AppSettings>(jsonContent, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    
                    if (externalSettings != null)
                    {
                        logger?.LogInformation("Configurações externas carregadas com sucesso");
                        return externalSettings;
                    }
                    else
                    {
                        logger?.LogWarning("Falha ao deserializar configurações externas, usando padrão");
                    }
                }
                else
                {
                    logger?.LogInformation("Arquivo de configuração não encontrado, usando configurações embutidas");
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Erro ao carregar configurações externas, usando padrão");
            }
            
            // Se não conseguir carregar do arquivo, usa as configurações embutidas
            logger?.LogInformation("Usando configurações embutidas padrão");
            return EmbeddedSettings.GetDefaultSettings();
        }
        
        /// <summary>
        /// Salva configurações em arquivo externo
        /// </summary>
        /// <param name="settings">Configurações para salvar</param>
        /// <param name="logger">Logger para registrar operações</param>
        /// <returns>True se salvou com sucesso</returns>
        public static bool SaveSettings(AppSettings settings, ILogger? logger = null)
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CONFIG_FILE_NAME);
                
                var jsonContent = JsonSerializer.Serialize(settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(configPath, jsonContent);
                
                logger?.LogInformation("Configurações salvas em: {ConfigPath}", configPath);
                return true;
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Erro ao salvar configurações");
                return false;
            }
        }
        
        /// <summary>
        /// Cria um arquivo de configuração de exemplo
        /// </summary>
        /// <param name="logger">Logger para registrar operações</param>
        /// <returns>True se criou com sucesso</returns>
        public static bool CreateExampleConfig(ILogger? logger = null)
        {
            try
            {
                var exampleSettings = EmbeddedSettings.GetCustomizedSettings("MeuServidor", "https://meuservidor.com");
                return SaveSettings(exampleSettings, logger);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Erro ao criar arquivo de configuração de exemplo");
                return false;
            }
        }
    }
}
