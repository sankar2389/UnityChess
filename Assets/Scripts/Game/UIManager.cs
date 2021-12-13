﻿using System;
using System.Collections.Generic;
using UnityChess;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviourSingleton<UIManager> {
	[SerializeField] private GameObject promotionUI = null;
	[SerializeField] private Text resultText = null;
	[SerializeField] private InputField GameStringInputField = null;
	[SerializeField] private Image whiteTurnIndicator = null;
	[SerializeField] private Image blackTurnIndicator = null;
	[SerializeField] private GameObject moveHistoryContentParent = null;
	[SerializeField] private Scrollbar moveHistoryScrollbar = null;
	[SerializeField] private GameObject moveUIPrefab = null;
	[SerializeField] private Text[] boardInfoTexts = null;
	[SerializeField] private Color backgroundColor = new Color(0.39f, 0.39f, 0.39f);
	[SerializeField] private Color textColor = new Color(1f, 0.71f, 0.18f);
	[SerializeField, Range(-0.25f, 0.25f)] private float buttonColorDarkenAmount = 0f;
	[SerializeField, Range(-0.25f, 0.25f)] private float moveHistoryAlternateColorDarkenAmount = 0f;
	
	private Timeline<FullMoveUI> moveUITimeline;
	private Color buttonColor;

	private void Start() {
		GameManager.Instance.NewGameStarted += OnNewGameStarted;
		GameManager.Instance.GameEnded += OnGameEnded;
		GameManager.Instance.MoveExecuted += OnMoveExecuted;
		GameManager.Instance.GameResetToHalfMove += OnGameResetToHalfMove;
		
		moveUITimeline = new Timeline<FullMoveUI>();
		foreach (Text boardInfoText in boardInfoTexts) {
			boardInfoText.color = textColor;
		}

		buttonColor = new Color(backgroundColor.r - buttonColorDarkenAmount, backgroundColor.g - buttonColorDarkenAmount, backgroundColor.b - buttonColorDarkenAmount);
	}

	private void OnNewGameStarted() {
		UpdateGameStringInputField();
		ValidateIndicators();
		
		for (int i = 0; i < moveHistoryContentParent.transform.childCount; i++) {
			Destroy(moveHistoryContentParent.transform.GetChild(i).gameObject);
		}
		
		moveUITimeline.Clear();

		resultText.gameObject.SetActive(false);
	}

	private void OnGameEnded() {
		HalfMove latestHalfMove = GameManager.Instance.HalfMoveTimeline.Current;

		if (latestHalfMove.CausedCheckmate) resultText.text = $"{latestHalfMove.Piece.OwningSide} Wins!";
		else if (latestHalfMove.CausedStalemate) resultText.text = "Draw.";

		resultText.gameObject.SetActive(true);
	}

	private void OnMoveExecuted() {
		UpdateGameStringInputField();
		whiteTurnIndicator.enabled = !whiteTurnIndicator.enabled;
		blackTurnIndicator.enabled = !blackTurnIndicator.enabled;

		AddMoveToHistory(GameManager.Instance.HalfMoveTimeline.Current, GameManager.Instance.SideToMove.Complement());
	}

	private void OnGameResetToHalfMove() {
		UpdateGameStringInputField();
		moveUITimeline.HeadIndex = GameManager.Instance.HalfMoveCount / 2;
		ValidateIndicators();
	}

	public void SetActivePromotionUI(bool value) => promotionUI.gameObject.SetActive(value);

	public void OnElectionButton(int choice) => GameManager.Instance.ElectPiece((ElectedPiece)choice);

	public void ResetGameToFirstHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(0);

	public void ResetGameToPreviousHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(Math.Max(0, GameManager.Instance.HalfMoveCount - 1));

	public void ResetGameToNextHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(Math.Min(GameManager.Instance.HalfMoveCount + 1, GameManager.Instance.HalfMoveTimeline.Count - 1));

	public void ResetGameToLastHalfMove() => GameManager.Instance.ResetGameToHalfMoveIndex(GameManager.Instance.HalfMoveTimeline.Count - 1);

	public void StartNewGame(int mode) => GameManager.Instance.StartNewGame((Mode) mode);

	private void AddMoveToHistory(HalfMove latestHalfMove, Side latestTurnSide) {
		RemoveAlternateHistory();
		
		switch (latestTurnSide) {
			case Side.Black:
				FullMoveUI latestFullMoveUI = moveUITimeline.Current;
				latestFullMoveUI.BlackMoveText.text = latestHalfMove.ToAlgebraicNotation();
				latestFullMoveUI.BlackMoveButton.enabled = true;
				
				break;
			case Side.White:
				GameObject newMoveUIGO = Instantiate(moveUIPrefab, moveHistoryContentParent.transform);
				FullMoveUI newFullMoveUI = newMoveUIGO.GetComponent<FullMoveUI>();
				newFullMoveUI.backgroundImage.color = backgroundColor;
				newFullMoveUI.whiteMoveButtonImage.color = buttonColor;
				newFullMoveUI.blackMoveButtonImage.color = buttonColor;

				if (newFullMoveUI.FullMoveNumber % 2 == 0) newFullMoveUI.SetAlternateColor(moveHistoryAlternateColorDarkenAmount);
				newFullMoveUI.MoveNumberText.text = $"{newFullMoveUI.FullMoveNumber}.";
				newFullMoveUI.WhiteMoveText.text = latestHalfMove.ToAlgebraicNotation();
				newFullMoveUI.BlackMoveText.text = "";
				newFullMoveUI.BlackMoveButton.enabled = false;
				newFullMoveUI.WhiteMoveButton.enabled = true;
				
				moveUITimeline.AddNext(newFullMoveUI);
				break;
		}

		moveHistoryScrollbar.value = 0;
	}

	private void RemoveAlternateHistory() {
		if (!moveUITimeline.IsUpToDate) {
			resultText.gameObject.SetActive(GameManager.Instance.HalfMoveTimeline.Current.CausedCheckmate);
			List<FullMoveUI> divergentFullMoveUIs = moveUITimeline.PopFuture();
			foreach (FullMoveUI divergentFullMoveUI in divergentFullMoveUIs) Destroy(divergentFullMoveUI.gameObject);
		}
	}

	private void ValidateIndicators() {
		Side sideToMove = GameManager.Instance.SideToMove;
		whiteTurnIndicator.enabled = sideToMove == Side.White;
		blackTurnIndicator.enabled = sideToMove == Side.Black;
	}

	private void UpdateGameStringInputField() => GameStringInputField.text = GameManager.Instance.SerializeGame();
}