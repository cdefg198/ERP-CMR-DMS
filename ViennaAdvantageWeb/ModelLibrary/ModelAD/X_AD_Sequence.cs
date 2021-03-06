namespace VAdvantage.Model
{

/** Generated Model - DO NOT CHANGE */
using System;
using System.Text;
using VAdvantage.DataBase;
using VAdvantage.Common;
using VAdvantage.Classes;
using VAdvantage.Process;
using VAdvantage.Model;
using VAdvantage.Utility;
using System.Data;
/** Generated Model for AD_Sequence
 *  @author Jagmohan Bhatt (generated) 
 *  @version Vienna Framework 1.1.1 - $Id$ */
public class X_AD_Sequence : PO
{
public X_AD_Sequence (Context ctx, int AD_Sequence_ID, Trx trxName) : base (ctx, AD_Sequence_ID, trxName)
{
/** if (AD_Sequence_ID == 0)
{
SetAD_Sequence_ID (0);
SetCurrentNext (0);	// 1000000
SetCurrentNextSys (0);	// 100
SetIncrementNo (0);	// 1
SetIsAutoSequence (false);
SetName (null);
SetStartNo (0);	// 1000000
}
 */
}
public X_AD_Sequence (Ctx ctx, int AD_Sequence_ID, Trx trxName) : base (ctx, AD_Sequence_ID, trxName)
{
/** if (AD_Sequence_ID == 0)
{
SetAD_Sequence_ID (0);
SetCurrentNext (0);	// 1000000
SetCurrentNextSys (0);	// 100
SetIncrementNo (0);	// 1
SetIsAutoSequence (false);
SetName (null);
SetStartNo (0);	// 1000000
}
 */
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_AD_Sequence (Context ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_AD_Sequence (Ctx ctx, DataRow rs, Trx trxName) : base(ctx, rs, trxName)
{
}
/** Load Constructor 
@param ctx context
@param rs result set 
@param trxName transaction
*/
public X_AD_Sequence (Ctx ctx, IDataReader dr, Trx trxName) : base(ctx, dr, trxName)
{
}
/** Static Constructor 
 Set Table ID By Table Name
 added by ->Harwinder */
static X_AD_Sequence()
{
 Table_ID = Get_Table_ID(Table_Name);
 model = new KeyNamePair(Table_ID,Table_Name);
}
/** Serial Version No */
////static long serialVersionUID = 27562514364053L;
/** Last Updated Timestamp 7/29/2010 1:07:27 PM */
public static long updatedMS = 1280389047264L;
/** AD_Table_ID=115 */
public static int Table_ID;
 // =115;

/** TableName=AD_Sequence */
public static String Table_Name="AD_Sequence";

protected static KeyNamePair model;
protected Decimal accessLevel = new Decimal(6);
/** AccessLevel
@return 6 - System - Client 
*/
protected override int Get_AccessLevel()
{
return Convert.ToInt32(accessLevel.ToString());
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO (Ctx ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Load Meta Data
@param ctx context
@return PO Info
*/
protected override POInfo InitPO(Context ctx)
{
POInfo poi = POInfo.GetPOInfo (ctx, Table_ID);
return poi;
}
/** Info
@return info
*/
public override String ToString()
{
StringBuilder sb = new StringBuilder ("X_AD_Sequence[").Append(Get_ID()).Append("]");
return sb.ToString();
}
/** Set Sequence.
@param AD_Sequence_ID Document Sequence */
public void SetAD_Sequence_ID (int AD_Sequence_ID)
{
if (AD_Sequence_ID < 1) throw new ArgumentException ("AD_Sequence_ID is mandatory.");
Set_ValueNoCheck ("AD_Sequence_ID", AD_Sequence_ID);
}
/** Get Sequence.
@return Document Sequence */
public int GetAD_Sequence_ID() 
{
Object ii = Get_Value("AD_Sequence_ID");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Current Next.
@param CurrentNext The next number to be used */
public void SetCurrentNext (int CurrentNext)
{
Set_Value ("CurrentNext", CurrentNext);
}
/** Get Current Next.
@return The next number to be used */
public int GetCurrentNext() 
{
Object ii = Get_Value("CurrentNext");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Current Next (System).
@param CurrentNextSys Next sequence for system use */
public void SetCurrentNextSys (int CurrentNextSys)
{
Set_Value ("CurrentNextSys", CurrentNextSys);
}
/** Get Current Next (System).
@return Next sequence for system use */
public int GetCurrentNextSys() 
{
Object ii = Get_Value("CurrentNextSys");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Description.
@param Description Optional short description of the record */
public void SetDescription (String Description)
{
if (Description != null && Description.Length > 255)
{
log.Warning("Length > 255 - truncated");
Description = Description.Substring(0,255);
}
Set_Value ("Description", Description);
}
/** Get Description.
@return Optional short description of the record */
public String GetDescription() 
{
return (String)Get_Value("Description");
}
/** Set Increment.
@param IncrementNo The number to increment the last document number by */
public void SetIncrementNo (int IncrementNo)
{
Set_Value ("IncrementNo", IncrementNo);
}
/** Get Increment.
@return The number to increment the last document number by */
public int GetIncrementNo() 
{
Object ii = Get_Value("IncrementNo");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Activate Audit.
@param IsAudited Activate Audit Trail of what numbers are generated */
public void SetIsAudited (Boolean IsAudited)
{
Set_Value ("IsAudited", IsAudited);
}
/** Get Activate Audit.
@return Activate Audit Trail of what numbers are generated */
public Boolean IsAudited() 
{
Object oo = Get_Value("IsAudited");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Auto numbering.
@param IsAutoSequence Automatically assign the next number */
public void SetIsAutoSequence (Boolean IsAutoSequence)
{
Set_Value ("IsAutoSequence", IsAutoSequence);
}
/** Get Auto numbering.
@return Automatically assign the next number */
public Boolean IsAutoSequence() 
{
Object oo = Get_Value("IsAutoSequence");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Used for Record ID.
@param IsTableID The document number  will be used as the record key */
public void SetIsTableID (Boolean IsTableID)
{
Set_Value ("IsTableID", IsTableID);
}
/** Get Used for Record ID.
@return The document number  will be used as the record key */
public Boolean IsTableID() 
{
Object oo = Get_Value("IsTableID");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Name.
@param Name Alphanumeric identifier of the entity */
public void SetName (String Name)
{
if (Name == null) throw new ArgumentException ("Name is mandatory.");
if (Name.Length > 60)
{
log.Warning("Length > 60 - truncated");
Name = Name.Substring(0,60);
}
Set_Value ("Name", Name);
}
/** Get Name.
@return Alphanumeric identifier of the entity */
public String GetName() 
{
return (String)Get_Value("Name");
}
/** Get Record ID/ColumnName
@return ID/ColumnName pair */
public KeyNamePair GetKeyNamePair() 
{
return new KeyNamePair(Get_ID(), GetName());
}
/** Set Prefix.
@param Prefix Prefix before the sequence number */
public void SetPrefix (String Prefix)
{
if (Prefix != null && Prefix.Length > 10)
{
log.Warning("Length > 10 - truncated");
Prefix = Prefix.Substring(0,10);
}
Set_Value ("Prefix", Prefix);
}
/** Get Prefix.
@return Prefix before the sequence number */
public String GetPrefix() 
{
return (String)Get_Value("Prefix");
}
/** Set Restart sequence every Year.
@param StartNewYear Restart the sequence with Start on every 1/1 */
public void SetStartNewYear (Boolean StartNewYear)
{
Set_Value ("StartNewYear", StartNewYear);
}
/** Get Restart sequence every Year.
@return Restart the sequence with Start on every 1/1 */
public Boolean IsStartNewYear() 
{
Object oo = Get_Value("StartNewYear");
if (oo != null) 
{
 if (oo.GetType() == typeof(bool)) return Convert.ToBoolean(oo);
 return "Y".Equals(oo);
}
return false;
}
/** Set Start No.
@param StartNo Starting number/position */
public void SetStartNo (int StartNo)
{
Set_Value ("StartNo", StartNo);
}
/** Get Start No.
@return Starting number/position */
public int GetStartNo() 
{
Object ii = Get_Value("StartNo");
if (ii == null) return 0;
return Convert.ToInt32(ii);
}
/** Set Suffix.
@param Suffix Suffix after the number */
public void SetSuffix (String Suffix)
{
if (Suffix != null && Suffix.Length > 10)
{
log.Warning("Length > 10 - truncated");
Suffix = Suffix.Substring(0,10);
}
Set_Value ("Suffix", Suffix);
}
/** Get Suffix.
@return Suffix after the number */
public String GetSuffix() 
{
return (String)Get_Value("Suffix");
}
/** Set Value Format.
@param VFormat Format of the value;
 Can contain fixed format elements, Variables: "_lLoOaAcCa09" */
public void SetVFormat (String VFormat)
{
if (VFormat != null && VFormat.Length > 40)
{
log.Warning("Length > 40 - truncated");
VFormat = VFormat.Substring(0,40);
}
Set_Value ("VFormat", VFormat);
}
/** Get Value Format.
@return Format of the value;
 Can contain fixed format elements, Variables: "_lLoOaAcCa09" */
public String GetVFormat() 
{
return (String)Get_Value("VFormat");
}
}

}
