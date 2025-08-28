namespace Gateway.Abstractions
{
    public interface IGatewayRouter
    {
        /// <summary>
        /// Résout le nom de queue RabbitMQ à partir du segment {game}.
        /// </summary>
        string ResolveQueue(string game);

        /// <summary>
        /// Vérifie si la table est autorisée pour le jeu donné.
        /// </summary>
        bool IsTableAllowed(string game, string table);

        /// <summary>
        /// Vérifie si l'action est autorisée pour cette table
        /// </summary>
        bool IsActionAllowed(string game, string table, string action);
    }
}
