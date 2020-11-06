# Analyzer

In a larger solution of C# projects it can become difficult 
to get an overview of the interdependencies between the various
projects. 

* The list of project references in the csproj file may be misleading
  because on the references packages might not be used.
* By referencing a project A the project inherits types defined 
  in projects that are referenced by A.
* If a project references another, how many types are actually used?
 
Analyer aims to help analyze these dependencies with the help of Roslyn.
The goal is also to make Analyzer run on Windows and Linux.





