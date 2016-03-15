﻿'Open VOGEL (www.openvogel.com)
'Open source software for aerodynamics
'Copyright (C) 2016 Guillermo Hazebrouck (guillermo.hazebrouck@openvogel.com)

'This program Is free software: you can redistribute it And/Or modify
'it under the terms Of the GNU General Public License As published by
'the Free Software Foundation, either version 3 Of the License, Or
'(at your option) any later version.

'This program Is distributed In the hope that it will be useful,
'but WITHOUT ANY WARRANTY; without even the implied warranty Of
'MERCHANTABILITY Or FITNESS FOR A PARTICULAR PURPOSE.  See the
'GNU General Public License For more details.

'You should have received a copy Of the GNU General Public License
'along with this program.  If Not, see < http:  //www.gnu.org/licenses/>.

Imports AeroTools.UVLM.Models.Aero.Components

Public Class FormPolarCurve

    Public Sub New(PolarDataBase As PolarDatabase, SelectedFamiyID As Guid)

        InitializeComponent()

        DataBase = PolarDataBase

        RefreshFamilyList()

        QuadraticFrame.Hide()
        QuadraticFrame.Parent = Me
        CustomFrame.Hide()
        CustomFrame.Parent = Me

        PolarPlotter = New PolarPlotter()
        PolarPlotter.Parent = Me
        PolarPlotter.Visible = False
        AddHandler PolarPlotter.PointChanged, AddressOf CustomFrame.RefreshTable

        If lbFamilies.Items.Count > 0 Then
            For i = 0 To PolarDataBase.Families.Count - 1
                If PolarDataBase.Families(i).ID.Equals(SelectedFamiyID) Then
                    lbFamilies.SelectedIndex = i
                    Exit For
                End If
            Next
        End If
        If lbPolars.Items.Count > 0 Then lbPolars.SelectedIndex = 0

        AddHandler CustomFrame.OnNodeChanged, AddressOf PolarPlotter.Refresh

    End Sub

    Public DataBase As PolarDatabase

    Private CurrentFamily As PolarFamily
    Private CurrentPolar As IPolarCurve

    Public ReadOnly Property SelectedFamilyID As Guid
        Get
            If IsNothing(CurrentFamily) Then
                Return Guid.Empty
            Else
                Return CurrentFamily.ID
            End If
        End Get
    End Property

    Public ReadOnly Property SelectedPolarID As Guid
        Get
            If IsNothing(CurrentPolar) Then
                Return Guid.Empty
            Else
                Return CurrentPolar.ID
            End If
        End Get
    End Property

    Private QuadraticFrame As New QuadraticPolarControl
    Private CustomFrame As New CustomPolarControl
    Private PolarPlotter As PolarPlotter

    Private LockEvents As Boolean = False

    Private Sub RefreshFamilyList()

        LockEvents = True

        lbFamilies.Items.Clear()

        If Not IsNothing(DataBase) Then
            For Each Family In DataBase.Families
                Family.SortPolars()
                lbFamilies.Items.Add(Family.Name)
            Next
        End If

        LockEvents = False

    End Sub

    Private Sub RefreshPolarsList()

        LockEvents = True

        lbPolars.Items.Clear()

        If Not IsNothing(CurrentFamily) Then

            For Each Polar In CurrentFamily.Polars
                lbPolars.Items.Add(String.Format("{0} - (Re = {1:E3})", Polar.Name, Polar.Reynolds))
            Next

        End If

        QuadraticFrame.Hide()
        CustomFrame.Hide()

        LockEvents = False

    End Sub

    Private Sub SelectFamily(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbFamilies.SelectedIndexChanged

        If Not LockEvents Then

            If lbFamilies.SelectedIndex >= 0 And lbFamilies.SelectedIndex < DataBase.Families.Count Then
                CurrentFamily = DataBase.Families(lbFamilies.SelectedIndex)
                CurrentPolar = Nothing
                RefreshPolarsList()
            End If

        End If

    End Sub

    Private Sub SelectPolar(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles lbPolars.SelectedIndexChanged

        If Not LockEvents And CurrentFamily IsNot Nothing Then

            If lbPolars.SelectedIndex >= 0 And lbPolars.SelectedIndex <= CurrentFamily.Polars.Count Then

                CurrentPolar = CurrentFamily.Polars(lbPolars.SelectedIndex)

                Select Case CurrentPolar.Type

                    Case PolarType.Quadratic

                        CustomFrame.Hide()
                        QuadraticFrame.Polar = CurrentPolar
                        QuadraticFrame.Left = Width - QuadraticFrame.Width - 20
                        QuadraticFrame.Top = lbPolars.Top
                        QuadraticFrame.Show()

                        PolarPlotter.Left = lbPolars.Right + 5
                        PolarPlotter.Width = QuadraticFrame.Left - lbPolars.Right - 10
                        PolarPlotter.Top = lbPolars.Top
                        PolarPlotter.Height = PolarPlotter.Width
                        PolarPlotter.Visible = True

                    Case PolarType.Custom

                        QuadraticFrame.Hide()
                        CustomFrame.Polar = CurrentPolar
                        CustomFrame.Left = Width - CustomFrame.Width - 20
                        CustomFrame.Top = lbPolars.Top
                        CustomFrame.Height = btnOK.Bottom - CustomFrame.Top
                        CustomFrame.Show()

                        PolarPlotter.Left = lbPolars.Right + 5
                        PolarPlotter.Width = CustomFrame.Left - lbPolars.Right - 10
                        PolarPlotter.Top = lbPolars.Top
                        PolarPlotter.Height = PolarPlotter.Width
                        PolarPlotter.Visible = True

                    Case Else

                        PolarPlotter.Visible = False
                        PolarPlotter.Polar = Nothing

                End Select

                PolarPlotter.Polar = CurrentPolar
                PolarPlotter.Refresh()

            End If

        End If

    End Sub

    Private Sub SavePolarDatabase(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnSavePolarDB.Click

        If sfdSavePolarDB.ShowDialog() = vbOK Then
            DataBase.Path = sfdSavePolarDB.FileName
            DataBase.WriteBinary()
        End If

    End Sub

    Private Sub LoadPolarDatabase(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles bnLoadPolarDB.Click

        If ofdLoadPolarDB.ShowDialog = vbOK Then
            DataBase.Path = ofdLoadPolarDB.FileName
            Try
                DataBase.ReadBinary()
                RefreshFamilyList()
                RefreshPolarsList()
            Catch ex As Exception
                MsgBox("Unable to load polars database from selected file!")
            End Try
        End If

    End Sub

    Private Sub RemoveSelectedFamily(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRemoveFamily.Click
        If (lbFamilies.SelectedIndex >= 0) And (lbFamilies.SelectedIndex < DataBase.Families.Count) Then
            DataBase.Families.RemoveAt(lbFamilies.SelectedIndex)
            RefreshFamilyList()
            RefreshPolarsList()
        End If
    End Sub

    Private Sub RemoveSelectedPolar(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles btnRemovePolar.Click
        If Not IsNothing(CurrentFamily) Then
            If (lbFamilies.SelectedIndex >= 0 And lbPolars.SelectedIndex < CurrentFamily.Polars.Count) Then
                CurrentFamily.Polars.RemoveAt(lbPolars.SelectedIndex)
                RefreshPolarsList()
            End If
        End If
    End Sub

    Private Sub AddFamily() Handles btnAddFamily.Click
        Dim Family As New PolarFamily()
        Family.Name = "Polar family"
        DataBase.Families.Add(Family)
        RefreshFamilyList()
        RefreshPolarsList()
    End Sub

    Private Sub AddQuadraticPolar() Handles btnAddQuadratic.Click

        If Not IsNothing(CurrentFamily) Then
            CurrentFamily.Polars.Add(New QuadraticPolar())
            RefreshPolarsList()
        End If

    End Sub

    Private Sub AddCustomPolar() Handles btnAddCustom.Click

        If Not IsNothing(CurrentFamily) Then
            CurrentFamily.Polars.Add(New CustomPolar())
            RefreshPolarsList()
        End If

    End Sub

End Class