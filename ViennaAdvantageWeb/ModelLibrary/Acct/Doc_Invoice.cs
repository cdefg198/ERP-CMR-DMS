﻿/********************************************************
 * Project Name   : VAdvantage
 * Class Name     : Doc_Invoice
 * Purpose        : Post Invoice Documents.
 *                  <pre>
 *                  Table:    C_Invoice (318)
 *                  Document Types:     ARI, ARC, ARF, API, APC
 *  </pre>
 *  * Class Used     : Doc
 * Chronological    Development
 * Raghunandan      19-Jan-2010
  ******************************************************/
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VAdvantage.Classes;
using VAdvantage.Common;
using VAdvantage.Process;
using System.Windows.Forms;
using VAdvantage.Model;
using VAdvantage.DataBase;
using VAdvantage.SqlExec;
using VAdvantage.Utility;
using System.Data;
using VAdvantage.Logging;
using System.Data.SqlClient;
using VAdvantage.Acct;

namespace VAdvantage.Acct
{
    public class Doc_Invoice : Doc
    {
        #region
        //Contained Optional Tax Lines    
        private DocTax[] _taxes = null;
        //Currency Precision				
        private int _precision = -1;
        // All lines are Service			
        private bool _allLinesService = true;
        //All lines are product item		
        private bool _allLinesItem = true;

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="ass"></param>
        /// <param name="idr"></param>
        /// <param name="trxName"></param>
        public Doc_Invoice(MAcctSchema[] ass, IDataReader idr, Trx trxName)
            : base(ass, typeof(MInvoice), idr, null, trxName)
        {

        }
        public Doc_Invoice(MAcctSchema[] ass, DataRow dr, Trx trxName)
            : base(ass, typeof(MInvoice), dr, null, trxName)
        {

        }

        /// <summary>
        /// Load Specific Document Details
        /// </summary>
        /// <returns>error message or null</returns>
        public override String LoadDocumentDetails()
        {
            MInvoice invoice = (MInvoice)GetPO();
            SetDateDoc(invoice.GetDateInvoiced());
            SetIsTaxIncluded(invoice.IsTaxIncluded());
            //	Amounts
            SetAmount(Doc.AMTTYPE_Gross, invoice.GetGrandTotal());
            SetAmount(Doc.AMTTYPE_Net, invoice.GetTotalLines());
            SetAmount(Doc.AMTTYPE_Charge, invoice.GetChargeAmt());

            //	Contained Objects
            _taxes = LoadTaxes();
            _lines = LoadLines(invoice);
            log.Fine("Lines=" + _lines.Length + ", Taxes=" + _taxes.Length);
            return null;
        }

        /// <summary>
        /// Load Invoice Taxes
        /// </summary>
        /// <returns>DocTax Array</returns>
        private DocTax[] LoadTaxes()
        {
            List<DocTax> list = new List<DocTax>();
            String sql = "SELECT it.C_Tax_ID, t.Name, t.Rate, it.TaxBaseAmt, it.TaxAmt, t.IsSalesTax "
                + "FROM C_Tax t, C_InvoiceTax it "
                + "WHERE t.C_Tax_ID=it.C_Tax_ID AND it.C_Invoice_ID=" + Get_ID();
            IDataReader idr = null;
            try
            {
                idr = DataBase.DB.ExecuteReader(sql, null, GetTrx());
                while (idr.Read())
                {
                    int C_Tax_ID = Utility.Util.GetValueOfInt(idr[0]);//.getInt(1);
                    String name = Utility.Util.GetValueOfString(idr[1]);//.getString(2);
                    Decimal rate = Utility.Util.GetValueOfDecimal(idr[2]);//.getBigDecimal(3);
                    Decimal taxBaseAmt = Utility.Util.GetValueOfDecimal(idr[3]);//.getBigDecimal(4);
                    Decimal amount = Utility.Util.GetValueOfDecimal(idr[4]);//.getBigDecimal(5);
                    bool salesTax = "Y".Equals(Utility.Util.GetValueOfString(idr[5]));//.getString(6));
                    //
                    DocTax taxLine = new DocTax(C_Tax_ID, name, rate, taxBaseAmt, amount, salesTax);
                    log.Fine(taxLine.ToString());
                    list.Add(taxLine);
                }
                //
                idr.Close();
            }
            catch (Exception e)
            {
                if (idr != null)
                {
                    idr.Close();
                    idr = null;
                }
                log.Log(Level.SEVERE, sql, e);
                return null;
            }

            //	Return Array
            DocTax[] tl = new DocTax[list.Count];
            tl = list.ToArray();
            return tl;
        }

        /// <summary>
        /// Load Invoice Line
        /// </summary>
        /// <param name="invoice">invoice</param>
        /// <returns>DocLine Array</returns>
        private DocLine[] LoadLines(MInvoice invoice)
        {
            List<DocLine> list = new List<DocLine>();
            //
            MInvoiceLine[] lines = invoice.GetLines(false);
            for (int i = 0; i < lines.Length; i++)
            {
                MInvoiceLine line = lines[i];
                if (line.IsDescription())
                {
                    continue;
                }
                DocLine docLine = new DocLine(line, this);
                //	Qty
                Decimal Qty = line.GetQtyInvoiced();
                bool cm = GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_ARCREDITMEMO)
                    || GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_APCREDITMEMO);
                docLine.SetQty(cm ? Decimal.Negate(Qty) : Qty, invoice.IsSOTrx());
                //
                Decimal LineNetAmt = line.GetLineNetAmt();
                Decimal PriceList = line.GetPriceList();
                int C_Tax_ID = docLine.GetC_Tax_ID();
                //	Correct included Tax
                if (IsTaxIncluded() && C_Tax_ID != 0)
                {
                    MTax tax = MTax.Get(GetCtx(), C_Tax_ID);
                    if (!tax.IsZeroTax())
                    {
                        Decimal LineNetAmtTax = tax.CalculateTax(LineNetAmt, true, GetStdPercision());
                        log.Fine("LineNetAmt=" + LineNetAmt + " - Tax=" + LineNetAmtTax);
                        LineNetAmt = Decimal.Subtract(LineNetAmt, LineNetAmtTax);
                        for (int t = 0; t < _taxes.Length; t++)
                        {
                            if (_taxes[t].GetC_Tax_ID() == C_Tax_ID)
                            {
                                _taxes[t].AddIncludedTax(LineNetAmtTax);
                                break;
                            }
                        }
                        Decimal PriceListTax = tax.CalculateTax(PriceList, true, GetStdPercision());
                        PriceList = Decimal.Subtract(PriceList, PriceListTax);
                    }
                }	//	correct included Tax

                docLine.SetAmount(LineNetAmt, PriceList, Qty);	//	qty for discount calc
                if (docLine.IsItem())
                {
                    _allLinesService = false;
                }
                else
                {
                    _allLinesItem = false;
                }
                //
                log.Fine(docLine.ToString());
                list.Add(docLine);
            }

            //	Convert to Array
            DocLine[] dls = new DocLine[list.Count];
            dls = list.ToArray();

            //	Included Tax - make sure that no difference
            if (IsTaxIncluded())
            {
                for (int i = 0; i < _taxes.Length; i++)
                {
                    if (_taxes[i].IsIncludedTaxDifference())
                    {
                        Decimal diff = _taxes[i].GetIncludedTaxDifference();
                        for (int j = 0; j < dls.Length; j++)
                        {
                            if (dls[j].GetC_Tax_ID() == _taxes[i].GetC_Tax_ID())
                            {
                                dls[j].SetLineNetAmtDifference(diff);
                                break;
                            }
                        }	//	for all lines
                    }	//	tax difference
                }	//	for all taxes
            }	//	Included Tax difference

            //	Return Array
            return dls;
        }

        /// <summary>
        /// Get Currency Percision
        /// </summary>
        /// <returns>precision</returns>
        private int GetStdPercision()
        {
            if (_precision == -1)
            {
                _precision = MCurrency.GetStdPrecision(GetCtx(), GetC_Currency_ID());
            }
            return _precision;
        }


        /// <summary>
        /// Get Source Currency Balance - subtracts line and tax amounts from total - no rounding
        /// </summary>
        /// <returns> positive amount, if total invoice is bigger than lines</returns>
        public override Decimal GetBalance()
        {
            Decimal retValue = Env.ZERO;
            StringBuilder sb = new StringBuilder(" [");
            //  Total
            retValue = Decimal.Add(retValue, GetAmount(Doc.AMTTYPE_Gross).Value);
            sb.Append(GetAmount(Doc.AMTTYPE_Gross));
            //  - Header Charge
            retValue = Decimal.Subtract(retValue, GetAmount(Doc.AMTTYPE_Charge).Value);
            sb.Append("-").Append(GetAmount(Doc.AMTTYPE_Charge));
            //  - Tax
            for (int i = 0; i < _taxes.Length; i++)
            {
                retValue = Decimal.Subtract(retValue, _taxes[i].GetAmount());
                sb.Append("-").Append(_taxes[i].GetAmount());
            }
            //  - Lines
            for (int i = 0; i < _lines.Length; i++)
            {
                retValue = Decimal.Subtract(retValue, _lines[i].GetAmtSource());
                sb.Append("-").Append(_lines[i].GetAmtSource());
            }
            sb.Append("]");
            //
            log.Fine(ToString() + " Balance=" + retValue + sb.ToString());
            return retValue;
        }

        /// <summary>
        /// Create Facts (the accounting logic) for
        /// ARI, ARC, ARF, API, APC.
        /// <pre>
        ///   ARI, ARF
        ///         Receivables     DR
        ///         Charge                  CR
        ///         TaxDue                  CR
        ///Revenue                 CR
        ///    ARC
        ///  Receivables             CR
        ///    Charge          DR
        ///    TaxDue          DR
        ///      Revenue         RR
        ///
        ///  API
        ///    Payables                CR
        ///    Charge          DR
        ///    TaxCredit       DR
        ///    Expense         DR
        ///
        ///  APC
        ///    Payables        DR
        ///     Charge                  CR
        ///    TaxCredit               CR
        ///     Expense                 CR
        /// </pre>
        /// </summary>
        /// <param name="as1">accounting schema</param>
        /// <returns> Fact</returns>
        public override List<Fact> CreateFacts(MAcctSchema as1)
        {
            //
            List<Fact> facts = new List<Fact>();
            //  create Fact Header
            Fact fact = new Fact(this, as1, Fact.POST_Actual);

            //  Cash based accounting
            if (!as1.IsAccrual())
            {
                return facts;
            }

            //  ** ARI, ARF
            if (GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_ARINVOICE)
                || GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_ARPROFORMAINVOICE))
            {
                Decimal grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                Decimal serviceAmt = Env.ZERO;

                //  Header Charge           CR
                Decimal amt = GetAmount(Doc.AMTTYPE_Charge).Value;
                if ( Env.Signum(amt) != 0)
                {
                    fact.CreateLine(null, GetAccount(Doc.ACCTTYPE_Charge, as1),
                        GetC_Currency_ID(), null, amt);
                }
                //  TaxDue                  CR
                for (int i = 0; i < _taxes.Length; i++)
                {
                    amt = _taxes[i].GetAmount();
                    if ( Env.Signum(amt) != 0)
                    {
                        FactLine tl = fact.CreateLine(null, _taxes[i].GetAccount(DocTax.ACCTTYPE_TaxDue, as1),
                            GetC_Currency_ID(), null, amt);
                        if (tl != null)
                        {
                            tl.SetC_Tax_ID(_taxes[i].GetC_Tax_ID());
                        }
                    }
                }
                //  Revenue                 CR
                for (int i = 0; i < _lines.Length; i++)
                {
                    amt = _lines[i].GetAmtSource();
                    Decimal? dAmt = null;
                    if (as1.IsTradeDiscountPosted())
                    {
                        Decimal discount = _lines[i].GetDiscount();
                        if ( Env.Signum(discount) != 0)
                        {
                            amt = Decimal.Add(amt, discount);
                            dAmt = discount;
                        }
                    }
                    fact.CreateLine(_lines[i],
                        _lines[i].GetAccount(ProductCost.ACCTTYPE_P_Revenue, as1),
                        GetC_Currency_ID(), dAmt, amt);
                    if (!_lines[i].IsItem())
                    {
                        grossAmt = Decimal.Subtract(grossAmt, amt);
                        serviceAmt = Decimal.Add(serviceAmt, amt);
                    }
                }
                //  Set Locations
                FactLine[] fLines = fact.GetLines();
                for (int i = 0; i < fLines.Length; i++)
                {
                    if (fLines[i] != null)
                    {
                        fLines[i].SetLocationFromOrg(fLines[i].GetAD_Org_ID(), true);      //  from Loc
                        fLines[i].SetLocationFromBPartner(GetC_BPartner_Location_ID(), false);  //  to Loc
                    }
                }

                //  Receivables     DR
                int receivables_ID = GetValidCombination_ID(Doc.ACCTTYPE_C_Receivable, as1);
                int receivablesServices_ID = GetValidCombination_ID(Doc.ACCTTYPE_C_Receivable_Services, as1);
                if (_allLinesItem || !as1.IsPostServices()
                    || receivables_ID == receivablesServices_ID)
                {
                    grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    serviceAmt = Env.ZERO;
                }
                else if (_allLinesService)
                {
                    serviceAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    grossAmt = Env.ZERO;
                }
                if (Env.Signum(grossAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), receivables_ID),
                        GetC_Currency_ID(), grossAmt, null);
                }
                if (Env.Signum(serviceAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), receivablesServices_ID),
                        GetC_Currency_ID(), serviceAmt, null);
                }
            }
            //  ARC
            else if (GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_ARCREDITMEMO))
            {
                Decimal grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                Decimal serviceAmt = Env.ZERO;

                //  Header Charge   DR
                Decimal amt = GetAmount(Doc.AMTTYPE_Charge).Value;
                if (Env.Signum(amt) != 0)
                {
                    fact.CreateLine(null, GetAccount(Doc.ACCTTYPE_Charge, as1),
                        GetC_Currency_ID(), amt, null);
                }
                //  TaxDue          DR
                for (int i = 0; i < _taxes.Length; i++)
                {
                    amt = _taxes[i].GetAmount();
                    if ( Env.Signum(amt) != 0)
                    {
                        FactLine tl = fact.CreateLine(null, _taxes[i].GetAccount(DocTax.ACCTTYPE_TaxDue, as1),
                            GetC_Currency_ID(), amt, null);
                        if (tl != null)
                        {
                            tl.SetC_Tax_ID(_taxes[i].GetC_Tax_ID());
                        }
                    }
                }
                //  Revenue         CR
                for (int i = 0; i < _lines.Length; i++)
                {
                    amt = _lines[i].GetAmtSource();
                    Decimal? dAmt = null;
                    if (as1.IsTradeDiscountPosted())
                    {
                        Decimal discount = _lines[i].GetDiscount();
                        if ( Env.Signum(discount) != 0)
                        {
                            amt = Decimal.Add(amt, discount);
                            dAmt = discount;
                        }
                    }
                    fact.CreateLine(_lines[i],
                        _lines[i].GetAccount(ProductCost.ACCTTYPE_P_Revenue, as1),
                        GetC_Currency_ID(), amt, dAmt);
                    if (!_lines[i].IsItem())
                    {
                        grossAmt = Decimal.Subtract(grossAmt, amt);
                        serviceAmt = Decimal.Add(serviceAmt, amt);
                    }
                }
                //  Set Locations
                FactLine[] fLines = fact.GetLines();
                for (int i = 0; i < fLines.Length; i++)
                {
                    if (fLines[i] != null)
                    {
                        fLines[i].SetLocationFromOrg(fLines[i].GetAD_Org_ID(), true);      //  from Loc
                        fLines[i].SetLocationFromBPartner(GetC_BPartner_Location_ID(), false);  //  to Loc
                    }
                }
                //  Receivables             CR
                int receivables_ID = GetValidCombination_ID(Doc.ACCTTYPE_C_Receivable, as1);
                int receivablesServices_ID = GetValidCombination_ID(Doc.ACCTTYPE_C_Receivable_Services, as1);
                if (_allLinesItem || !as1.IsPostServices()
                    || receivables_ID == receivablesServices_ID)
                {
                    grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    serviceAmt = Env.ZERO;
                }
                else if (_allLinesService)
                {
                    serviceAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    grossAmt = Env.ZERO;
                }
                if (Env.Signum(grossAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), receivables_ID),
                        GetC_Currency_ID(), null, grossAmt);
                }
                if (Env.Signum(serviceAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), receivablesServices_ID),
                        GetC_Currency_ID(), null, serviceAmt);
                }
            }

            //  ** API
            else if (GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_APINVOICE))
            {
                Decimal grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                Decimal serviceAmt = Env.ZERO;

                //  Charge          DR
                fact.CreateLine(null, GetAccount(Doc.ACCTTYPE_Charge, as1),
                    GetC_Currency_ID(), GetAmount(Doc.AMTTYPE_Charge), null);
                //  TaxCredit       DR
                for (int i = 0; i < _taxes.Length; i++)
                {
                    FactLine tl = fact.CreateLine(null, _taxes[i].GetAccount(_taxes[i].GetAPTaxType(), as1),
                        GetC_Currency_ID(), _taxes[i].GetAmount(), null);
                    if (tl != null)
                    {
                        tl.SetC_Tax_ID(_taxes[i].GetC_Tax_ID());
                    }
                }
                //  Expense         DR
                for (int i = 0; i < _lines.Length; i++)
                {
                    DocLine line = _lines[i];
                    bool landedCost = LandedCost(as1, fact, line, true);
                    if (landedCost && as1.IsExplicitCostAdjustment())
                    {
                        fact.CreateLine(line, line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1),
                            GetC_Currency_ID(), line.GetAmtSource(), null);
                        //
                        FactLine fl = fact.CreateLine(line, line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1),
                            GetC_Currency_ID(), null, line.GetAmtSource());
                        String desc = line.GetDescription();
                        if (desc == null)
                        {
                            desc = "100%";
                        }
                        else
                        {
                            desc += " 100%";
                        }
                        fl.SetDescription(desc);
                    }
                    if (!landedCost)
                    {
                        MAccount expense = line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1);
                        if (line.IsItem())
                        {
                            expense = line.GetAccount(ProductCost.ACCTTYPE_P_InventoryClearing, as1);
                        }
                        Decimal amt = line.GetAmtSource();
                        Decimal? dAmt = null;
                        if (as1.IsTradeDiscountPosted() && !line.IsItem())
                        {
                            Decimal discount = line.GetDiscount();
                            if ( Env.Signum(discount) != 0)
                            {
                                amt = Decimal.Add(amt, discount);
                                dAmt = discount;
                            }
                        }
                        fact.CreateLine(line, expense, GetC_Currency_ID(), amt, dAmt);
                        if (!line.IsItem())
                        {
                            grossAmt = Decimal.Subtract(grossAmt, amt);
                            serviceAmt = Decimal.Add(serviceAmt, amt);
                        }
                        //
                        if (line.GetM_Product_ID() != 0
                            && line.GetProduct().IsService())	//	otherwise Inv Matching
                        {
                            if (!IsPosted())
                            {

                                MCostDetail.CreateInvoice(as1, line.GetAD_Org_ID(),
                                    line.GetM_Product_ID(), line.GetM_AttributeSetInstance_ID(),
                                    line.Get_ID(), 0,		//	No Cost Element
                                    line.GetAmtSource(), line.GetQty().Value,
                                    line.GetDescription(), GetTrx(), GetRectifyingProcess());
                            }
                        }
                    }
                }
                //  Set Locations
                FactLine[] fLines = fact.GetLines();
                for (int i = 0; i < fLines.Length; i++)
                {
                    if (fLines[i] != null)
                    {
                        fLines[i].SetLocationFromBPartner(GetC_BPartner_Location_ID(), true);  //  from Loc
                        fLines[i].SetLocationFromOrg(fLines[i].GetAD_Org_ID(), false);    //  to Loc
                    }
                }

                //  Liability               CR
                int payables_ID = GetValidCombination_ID(Doc.ACCTTYPE_V_Liability, as1);
                int payablesServices_ID = GetValidCombination_ID(Doc.ACCTTYPE_V_Liability_Services, as1);
                if (_allLinesItem || !as1.IsPostServices()
                    || payables_ID == payablesServices_ID)
                {
                    grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    serviceAmt = Env.ZERO;
                }
                else if (_allLinesService)
                {
                    serviceAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    grossAmt = Env.ZERO;
                }
                if (Env.Signum(grossAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), payables_ID),
                        GetC_Currency_ID(), null, grossAmt);
                }
                if (Env.Signum(serviceAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), payablesServices_ID),
                        GetC_Currency_ID(), null, serviceAmt);
                }
                //
                UpdateProductPO(as1);	//	Only API
            }
            //  APC
            else if (GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_APCREDITMEMO))
            {
                Decimal grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                Decimal serviceAmt = Env.ZERO;
                //  Charge                  CR
                fact.CreateLine(null, GetAccount(Doc.ACCTTYPE_Charge, as1),
                    GetC_Currency_ID(), null, GetAmount(Doc.AMTTYPE_Charge));
                //  TaxCredit               CR
                for (int i = 0; i < _taxes.Length; i++)
                {
                    FactLine tl = fact.CreateLine(null, _taxes[i].GetAccount(_taxes[i].GetAPTaxType(), as1),
                        GetC_Currency_ID(), null, _taxes[i].GetAmount());
                    if (tl != null)
                    {
                        tl.SetC_Tax_ID(_taxes[i].GetC_Tax_ID());
                    }
                }
                //  Expense                 CR
                for (int i = 0; i < _lines.Length; i++)
                {
                    DocLine line = _lines[i];
                    bool landedCost = LandedCost(as1, fact, line, false);
                    if (landedCost && as1.IsExplicitCostAdjustment())
                    {
                        fact.CreateLine(line, line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1),
                            GetC_Currency_ID(), null, line.GetAmtSource());
                        //
                        FactLine fl = fact.CreateLine(line, line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1),
                            GetC_Currency_ID(), line.GetAmtSource(), null);
                        String desc = line.GetDescription();
                        if (desc == null)
                        {
                            desc = "100%";
                        }
                        else
                        {
                            desc += " 100%";
                        }
                        fl.SetDescription(desc);
                    }
                    if (!landedCost)
                    {
                        MAccount expense = line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1);
                        if (line.IsItem())
                        {
                            expense = line.GetAccount(ProductCost.ACCTTYPE_P_InventoryClearing, as1);
                        }
                        Decimal amt = line.GetAmtSource();
                        Decimal? dAmt = null;
                        if (as1.IsTradeDiscountPosted() && !line.IsItem())
                        {
                            Decimal discount = line.GetDiscount();
                            if ( Env.Signum(discount) != 0)
                            {
                                amt = Decimal.Add(amt, discount);
                                dAmt = discount;
                            }
                        }
                        fact.CreateLine(line, expense,
                            GetC_Currency_ID(), dAmt, amt);
                        if (!line.IsItem())
                        {
                            grossAmt = Decimal.Subtract(grossAmt, amt);
                            serviceAmt = Decimal.Add(serviceAmt, amt);
                        }
                        //
                        if (line.GetM_Product_ID() != 0
                            && line.GetProduct().IsService())	//	otherwise Inv Matching
                            if (!IsPosted())
                            {

                                MCostDetail.CreateInvoice(as1, line.GetAD_Org_ID(),
                                    line.GetM_Product_ID(), line.GetM_AttributeSetInstance_ID(),
                                    line.Get_ID(), 0,		//	No Cost Element
                                    Decimal.Negate(line.GetAmtSource()), line.GetQty().Value,
                                    line.GetDescription(), GetTrx(), GetRectifyingProcess());
                            }
                    }
                }
                //  Set Locations
                FactLine[] fLines = fact.GetLines();
                for (int i = 0; i < fLines.Length; i++)
                {
                    if (fLines[i] != null)
                    {
                        fLines[i].SetLocationFromBPartner(GetC_BPartner_Location_ID(), true);  //  from Loc
                        fLines[i].SetLocationFromOrg(fLines[i].GetAD_Org_ID(), false);    //  to Loc
                    }
                }
                //  Liability       DR
                int payables_ID = GetValidCombination_ID(Doc.ACCTTYPE_V_Liability, as1);
                int payablesServices_ID = GetValidCombination_ID(Doc.ACCTTYPE_V_Liability_Services, as1);
                if (_allLinesItem || !as1.IsPostServices()
                    || payables_ID == payablesServices_ID)
                {
                    grossAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    serviceAmt = Env.ZERO;
                }
                else if (_allLinesService)
                {
                    serviceAmt = GetAmount(Doc.AMTTYPE_Gross).Value;
                    grossAmt = Env.ZERO;
                }
                if (Env.Signum(grossAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), payables_ID),
                        GetC_Currency_ID(), grossAmt, null);
                }
                if (Env.Signum(serviceAmt) != 0)
                {
                    fact.CreateLine(null, MAccount.Get(GetCtx(), payablesServices_ID),
                        GetC_Currency_ID(), serviceAmt, null);
                }
            }
            else
            {
                _error = "DocumentType unknown: " + GetDocumentType();
                log.Log(Level.SEVERE, _error);
                fact = null;
            }
            //
            facts.Add(fact);
            return facts;
        }

        /// <summary>
        /// Create Fact Cash Based (i.e. only revenue/expense)
        /// </summary>
        /// <param name="as1">accounting schema</param>
        /// <param name="fact">fact to add lines to</param>
        /// <param name="multiplier">source amount multiplier</param>
        /// <returns>accounted amount</returns>
        public Decimal CreateFactCash(MAcctSchema as1, Fact fact, Decimal multiplier)
        {
            bool creditMemo = GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_ARCREDITMEMO)
                || GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_APCREDITMEMO);
            bool payables = GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_APINVOICE)
                || GetDocumentType().Equals(MDocBaseType.DOCBASETYPE_APCREDITMEMO);
            Decimal acctAmt = Env.ZERO;
            FactLine fl = null;
            //	Revenue/Cost
            for (int i = 0; i < _lines.Length; i++)
            {
                DocLine line = _lines[i];
                bool landedCost = false;
                if (payables)
                {
                    landedCost = LandedCost(as1, fact, line, false);
                }
                if (landedCost && as1.IsExplicitCostAdjustment())
                {
                    fact.CreateLine(line, line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1),
                        GetC_Currency_ID(), null, line.GetAmtSource());
                    //
                    fl = fact.CreateLine(line, line.GetAccount(ProductCost.ACCTTYPE_P_Expense, as1),
                        GetC_Currency_ID(), line.GetAmtSource(), null);
                    String desc = line.GetDescription();
                    if (desc == null)
                    {
                        desc = "100%";
                    }
                    else
                    {
                        desc += " 100%";
                    }
                    fl.SetDescription(desc);
                }
                if (!landedCost)
                {
                    MAccount acct = line.GetAccount(
                        payables ? ProductCost.ACCTTYPE_P_Expense : ProductCost.ACCTTYPE_P_Revenue, as1);
                    if (payables)
                    {
                        //	if Fixed Asset
                        if (line.IsItem())
                        {
                            acct = line.GetAccount(ProductCost.ACCTTYPE_P_InventoryClearing, as1);
                        }
                    }
                    Decimal? amt = (Decimal?)Decimal.Multiply(line.GetAmtSource(), multiplier);
                    Decimal? amt2 = null;
                    if (creditMemo)
                    {
                        amt2 = amt;
                        amt = null;
                    }
                    if (payables)	//	Vendor = DR
                    {
                        fl = fact.CreateLine(line, acct,
                            GetC_Currency_ID(), amt, amt2);
                    }
                    else			//	Customer = CR
                    {
                        fl = fact.CreateLine(line, acct,
                            GetC_Currency_ID(), amt2, amt);
                    }
                    if (fl != null)
                    {
                        acctAmt = Decimal.Add(acctAmt, fl.GetAcctBalance());
                    }
                }
            }
            //  Tax
            for (int i = 0; i < _taxes.Length; i++)
            {
                Decimal? amt =(Decimal?) _taxes[i].GetAmount();
                Decimal? amt2 = null;
                if (creditMemo)
                {
                    amt2 = amt;
                    amt = null;
                }
                FactLine tl = null;
                if (payables)
                {
                    tl = fact.CreateLine(null, _taxes[i].GetAccount(_taxes[i].GetAPTaxType(), as1),
                        GetC_Currency_ID(), amt, amt2);
                }
                else
                {
                    tl = fact.CreateLine(null, _taxes[i].GetAccount(DocTax.ACCTTYPE_TaxDue, as1),
                        GetC_Currency_ID(), amt2, amt);
                }
                if (tl != null)
                {
                    tl.SetC_Tax_ID(_taxes[i].GetC_Tax_ID());
                }
            }
            //  Set Locations
            FactLine[] fLines = fact.GetLines();
            for (int i = 0; i < fLines.Length; i++)
            {
                if (fLines[i] != null)
                {
                    if (payables)
                    {
                        fLines[i].SetLocationFromBPartner(GetC_BPartner_Location_ID(), true);  //  from Loc
                        fLines[i].SetLocationFromOrg(fLines[i].GetAD_Org_ID(), false);    //  to Loc
                    }
                    else
                    {
                        fLines[i].SetLocationFromOrg(fLines[i].GetAD_Org_ID(), true);    //  from Loc
                        fLines[i].SetLocationFromBPartner(GetC_BPartner_Location_ID(), false);  //  to Loc
                    }
                }
            }
            return acctAmt;
        }


        /// <summary>
        /// Create Landed Cost accounting & Cost lines
        /// </summary>
        /// <param name="as1">accounting schema</param>
        /// <param name="fact">fact</param>
        /// <param name="line">document line</param>
        /// <param name="dr">DR entry (normal api)</param>
        /// <returns>true if landed costs were created</returns>
        private bool LandedCost(MAcctSchema as1, Fact fact, DocLine line, bool dr)
        {
            int C_InvoiceLine_ID = line.Get_ID();
            MLandedCostAllocation[] lcas = MLandedCostAllocation.GetOfInvoiceLine(
                GetCtx(), C_InvoiceLine_ID, GetTrx());
            if (lcas.Length == 0)
            {
                return false;
            }

            //	Delete Old
            String sql = "DELETE FROM M_CostDetail WHERE C_InvoiceLine_ID=" + C_InvoiceLine_ID;
            int no = DataBase.DB.ExecuteQuery(sql, null, GetTrx());
            if (no != 0)
            {
                log.Config("CostDetail Deleted #" + no);
            }

            //	Calculate Total Base
            double totalBase = 0;
            for (int i = 0; i < lcas.Length; i++)
                totalBase += Convert.ToDouble(lcas[i].GetBase());//.doubleValue();

            //	Create New
            MInvoiceLine il = new MInvoiceLine(GetCtx(), C_InvoiceLine_ID, GetTrx());
            for (int i = 0; i < lcas.Length; i++)
            {
                MLandedCostAllocation lca = lcas[i];
                if (Env.Signum(lca.GetBase()) == 0)
                {
                    continue;
                }
                double percent = totalBase / Convert.ToDouble(lca.GetBase());
                String desc = il.GetDescription();
                if (desc == null)
                {
                    desc = percent + "%";
                }
                else
                {
                    desc += " - " + percent + "%";
                }
                if (line.GetDescription() != null)
                {
                    desc += " - " + line.GetDescription();
                }

                //	Accounting
                ProductCost pc = new ProductCost(GetCtx(),
                    lca.GetM_Product_ID(), lca.GetM_AttributeSetInstance_ID(), GetTrx());
                Decimal? drAmt = null;
                Decimal? crAmt = null;
                if (dr)
                {
                    drAmt = lca.GetAmt();
                }
                else
                {
                    crAmt = lca.GetAmt();
                }
                FactLine fl = fact.CreateLine(line, pc.GetAccount(ProductCost.ACCTTYPE_P_CostAdjustment, as1),
                    GetC_Currency_ID(), drAmt, crAmt);
                fl.SetDescription(desc);

                //	Cost Detail - Convert to AcctCurrency
                Decimal allocationAmt = lca.GetAmt();
                if (GetC_Currency_ID() != as1.GetC_Currency_ID())
                {
                    allocationAmt = MConversionRate.Convert(GetCtx(), allocationAmt,
                        GetC_Currency_ID(), as1.GetC_Currency_ID(),
                        GetDateAcct(), GetC_ConversionType_ID(),
                        GetAD_Client_ID(), GetAD_Org_ID());
                }
                if (Env.Scale(allocationAmt) > as1.GetCostingPrecision())
                {
                    allocationAmt = Decimal.Round(allocationAmt, as1.GetCostingPrecision(), MidpointRounding.AwayFromZero);
                }
                if (!dr)
                {
                    allocationAmt = Decimal.Negate(allocationAmt);
                }
                if (!IsPosted())
                {

                    MCostDetail cd = new MCostDetail(as1, lca.GetAD_Org_ID(),
                        lca.GetM_Product_ID(), lca.GetM_AttributeSetInstance_ID(),
                        lca.GetM_CostElement_ID(),
                        allocationAmt, lca.GetQty(),		//	Qty
                        desc, GetTrx());

                    cd.SetC_InvoiceLine_ID(C_InvoiceLine_ID);
                    bool ok = cd.Save();
                    if (ok && !cd.IsProcessed())
                    {
                        MClient client = MClient.Get(as1.GetCtx(), as1.GetAD_Client_ID());
                        if (client.IsCostImmediate())
                        {
                            cd.Process();
                        }
                    }
                }
            }

            log.Config("Created #" + lcas.Length);
            return true;
        }

        /// <summary>
        /// Update ProductPO PriceLastInv
        /// </summary>
        /// <param name="as1">accounting schema</param>
        private void UpdateProductPO(MAcctSchema as1)
        {
            MClientInfo ci = MClientInfo.Get(GetCtx(), as1.GetAD_Client_ID());
            if (ci.GetC_AcctSchema1_ID() != as1.GetC_AcctSchema_ID())
            {
                return;
            }

            StringBuilder sql = new StringBuilder(
                "UPDATE M_Product_PO po "
                + "SET PriceLastInv = "
                //	select
                + "(SELECT currencyConvert(il.PriceActual,i.C_Currency_ID,po.C_Currency_ID,i.DateInvoiced,i.C_ConversionType_ID,i.AD_Client_ID,i.AD_Org_ID) "
                + "FROM C_Invoice i, C_InvoiceLine il "
                + "WHERE i.C_Invoice_ID=il.C_Invoice_ID"
                + " AND po.M_Product_ID=il.M_Product_ID AND po.C_BPartner_ID=i.C_BPartner_ID");
            //jz + " AND ROWNUM=1 AND i.C_Invoice_ID=").Append(get_ID()).Append(") ")
            if (DataBase.DB.IsOracle()) //jz
            {
                sql.Append(" AND ROWNUM=1 ");
            }
            else
                sql.Append(" AND i.UPDATED IN (SELECT MAX(i1.UPDATED) "
                        + "FROM C_Invoice i1, C_InvoiceLine il1 "
                        + "WHERE i1.C_Invoice_ID=il1.C_Invoice_ID"
                        + " AND po.M_Product_ID=il1.M_Product_ID AND po.C_BPartner_ID=i1.C_BPartner_ID")
                        .Append("  AND i1.C_Invoice_ID=").Append(Get_ID()).Append(") ");
            sql.Append("  AND i.C_Invoice_ID=").Append(Get_ID()).Append(") ")
                //	update
            .Append("WHERE EXISTS (SELECT * "
            + "FROM C_Invoice i, C_InvoiceLine il "
            + "WHERE i.C_Invoice_ID=il.C_Invoice_ID"
            + " AND po.M_Product_ID=il.M_Product_ID AND po.C_BPartner_ID=i.C_BPartner_ID"
            + " AND i.C_Invoice_ID=").Append(Get_ID()).Append(")");

            int no = DataBase.DB.ExecuteQuery(sql.ToString(), null, GetTrx());
            log.Fine("Updated=" + no);
        }
    }
}
