using System.Xml.Linq;
using Microsoft.Dynamics.Nav.CodeAnalysis;

namespace Socitas.ReviewerCop.Common.Extensions;

public static class FileSystemInterfaceExtensions
{
    public static IEnumerable<XDocument> GetPermissionSetDocuments(this IFileSystem fileSystem) =>
        Microsoft.Dynamics.Nav.Analyzers.Common.FileSystemExtensions.GetPermissionSetDocuments(fileSystem);
}