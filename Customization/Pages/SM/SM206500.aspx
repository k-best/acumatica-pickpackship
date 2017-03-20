<%@ Page Language="C#" MasterPageFile="~/MasterPages/FormTab.master" AutoEventWireup="true"
	ValidateRequest="false" CodeFile="SM206500.aspx.cs" Inherits="Page_SM206500"
	Title="Untitled Page" %>

<%@ MasterType VirtualPath="~/MasterPages/FormTab.master" %>
<asp:Content ID="cont1" ContentPlaceHolderID="phDS" runat="Server">
	<px:PXDataSource ID="ds" runat="server" Visible="True" Width="100%" PrimaryView="Job"
		TypeName="PX.SM.PrintJobMaint">
		<CallbackCommands>
            <px:PXDSCallbackCommand Name="Insert" PostData="Self" />
			<px:PXDSCallbackCommand CommitChanges="True" Name="Save" />
			<px:PXDSCallbackCommand Name="First" PostData="Self" StartNewGroup="true" />
			<px:PXDSCallbackCommand Name="Last" PostData="Self" />
		</CallbackCommands>
	</px:PXDataSource>
</asp:Content>
<asp:content id="cont2" contentplaceholderid="phF" runat="Server">
	<px:PXFormView ID="form" runat="server" DataSourceID="ds" Height="63px" Width="100%" Visible="true" DataMember="Job">
        <Template>
            <px:PXLayoutRule runat="server" StartColumn="True" LabelsWidth="S" ControlSize="M" />
            <px:PXSelector ID="edPrintQueue" runat="server" DataField="PrintQueue" />
            <px:PXMaskEdit ID="edReportId" runat="server" DataField="ReportId" />
            <px:PXLayoutRule runat="server" ControlSize="XXL" />
            <px:PXTextEdit ID="edDescription" runat="server" DataField="Description" />
        </Template>
    </px:PXFormView>
</asp:content>
<asp:content id="cont3" contentplaceholderid="phG" runat="Server">
    <px:PXGrid ID="grid" runat="server" DataSourceID="ds" Style="height: 250px;" Width="100%" 
       SkinID="Details" Height="372px" TabIndex="-7372">
        <Levels>
            <px:PXGridLevel DataMember="Parameters" DataKeyNames="JobId,ParameterName">
                <Columns>
                    <px:PXGridColumn DataField="ParameterName" Width="200px" />
                    <px:PXGridColumn DataField="ParameterValue" Width="400px" />
                </Columns>
            </px:PXGridLevel>
        </Levels>
        <AutoSize Container="Window" Enabled="True" MinHeight="400" />
    </px:PXGrid>
</asp:content>