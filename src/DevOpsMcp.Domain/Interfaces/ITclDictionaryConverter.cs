using System.Collections.Generic;

namespace DevOpsMcp.Domain.Interfaces;

/// <summary>
/// Interface for converting between Tcl dictionaries and .NET objects
/// </summary>
public interface ITclDictionaryConverter
{
    /// <summary>
    /// Parses a Tcl dictionary string into a .NET dictionary
    /// </summary>
    Dictionary<string, object> ParseTclDictionary(string tclDict);
    
    /// <summary>
    /// Converts a .NET dictionary to a Tcl dictionary string
    /// </summary>
    string ToTclDictionary(Dictionary<string, object> dict);
    
    /// <summary>
    /// Parses a Tcl list string into a .NET list
    /// </summary>
    List<string> ParseTclList(string tclList);
    
    /// <summary>
    /// Converts a .NET list to a Tcl list string
    /// </summary>
    string ToTclList(IEnumerable<string> list);
    
    /// <summary>
    /// Tries to parse a string as various Tcl data structures
    /// </summary>
    object ParseTclValue(string value);
    
    /// <summary>
    /// Checks if a string represents a Tcl dictionary
    /// </summary>
    bool IsTclDictionary(string value);
    
    /// <summary>
    /// Checks if a string represents a Tcl list
    /// </summary>
    bool IsTclList(string value);
    
    /// <summary>
    /// Converts a Tcl dictionary to JSON string
    /// </summary>
    string ConvertTclDictToJson(string tclDict);
    
    /// <summary>
    /// Converts a Tcl list to JSON string
    /// </summary>
    string ConvertTclListToJson(string tclList);
}