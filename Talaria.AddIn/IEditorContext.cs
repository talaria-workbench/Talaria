namespace Talaria.AddIn;

public interface IEditorContext
{
    IEditorContext Root { get; }

    Task<Stream> CreateFileStream(string path);
    Task<Stream> OpenFileStream(string path);

    Task CreateFile<T>(string path, T item);
    Task<T> OpenFile<T>(string path);


    bool ExistsFileOrDirectory(string path);
    Task CreateFolder(string path);

}
