# hasher
A simple, portable, directory hashing tool

## Usage
```powershell
# create a direcotry hash file using MD5 (default) of the current directory
hasher.exe create
```

```powershell
# create a SHA256 directory hash file of a specific directory
hasher.exe create "C:\some\folder\path" --algorithm SHA256
```

```powershell
# validte a direcotory hash file in the current directory (MD5 by default)
hasher.exe validate
```
```powershell
# or specify a direcotry or algorithm
hasher.exe validate "C:\some\folder\path" --algorithm SHA256
```
