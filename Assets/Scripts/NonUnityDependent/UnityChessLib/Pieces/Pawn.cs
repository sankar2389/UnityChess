﻿using System.Collections.Generic;

namespace UnityChess {
	public class Pawn : Piece {
		public delegate ElectedPiece PieceChoiceAction();

		public static event PieceChoiceAction PawnPromoted;
		private static int instanceCounter;

		public Pawn(Square startingPosition, Side pieceOwner) : base(startingPosition, pieceOwner) {
			ID = ++instanceCounter;
		}

		private Pawn(Pawn pawnCopy) : base(pawnCopy) {
			ID = pawnCopy.ID;
		}

		public override void UpdateValidMoves(Board board, LinkedList<Turn> previousMoves, Side turn) {
			ValidMoves.Clear();

			CheckForwardMovingSquares(board, turn);
			CheckAttackingSquares(board, turn);
			CheckEnPassantCaptures(board, previousMoves, turn);
		}

		private void CheckForwardMovingSquares(Board board, Side turn) {
			Square testSquare = new Square(Position, 0, PieceOwner == Side.White ? 1 : -1);
			Movement testMove = new Movement(this, testSquare);
			
			if (!testSquare.IsOccupied(board) && Rules.MoveObeysRules(board, testMove, PieceOwner)) {
				if (Position.Rank == (PieceOwner == Side.White ? 7 : 2)) {
					// PSEUDO call to gui method which gets user promotion piece choice
					// ElectedPiece userElection = GUI.getElectionChoice();

					//for now will default to Queen election
					ElectedPiece userElection = ElectedPiece.Queen;
					ValidMoves.Add(new PromotionMove(new Square(testSquare), this, userElection));
				} else {
					ValidMoves.Add(new Movement(testMove));

					if (!HasMoved) {
						testSquare = new Square(testSquare, 0, PieceOwner == Side.White ? 1 : -1);
						testMove = new Movement(this, testSquare);
						if (!testSquare.IsOccupied(board) && Rules.MoveObeysRules(board, testMove, PieceOwner))
							ValidMoves.Add(new Movement(testMove));
					}
				}
			}
		}

		private void CheckAttackingSquares(Board board, Side turn) {
			Square testSquare = new Square(Position);
			Movement testMove = new Movement(this, testSquare);

			foreach (int fileOffset in new[] {-1, 1}) {
				int rankOffset = PieceOwner == Side.White ? 1 : -1;
				testSquare = new Square(Position, fileOffset, rankOffset);

				Square enemyKingPosition = PieceOwner == Side.White ? board.BlackKing.Position : board.WhiteKing.Position;
				if (testSquare.IsValid() && testSquare.IsOccupiedBySide(board, PieceOwner.Complement()) && Rules.MoveObeysRules(board, testMove, PieceOwner) && testSquare != enemyKingPosition) {
					if (Position.Rank == (PieceOwner == Side.White ? 7 : 2)) {
						// TODO subscribe to event
						ElectedPiece userElection = PawnPromoted.Invoke();
						
						ValidMoves.Add(new PromotionMove(new Square(testSquare), this, userElection));
					} else ValidMoves.Add(new Movement(testMove));
				}
			}
		}

		private void CheckEnPassantCaptures(Board board, LinkedList<Turn> previousMoves, Side turn) {
			if (PieceOwner == Side.White ? Position.Rank == 5 : Position.Rank == 4) {
				foreach (int fileOffset in new[] {-1, 1}) {
					Square testSquare = new Square(Position, fileOffset, 0);

					if (testSquare.IsValid() && board.GetPiece(testSquare) is Pawn enemyLateralPawn && enemyLateralPawn.PieceOwner != PieceOwner) {
						Piece lastMovedPiece = previousMoves.Last.Value.Piece;

						// TODO verify Equals call works 
						if (lastMovedPiece is Pawn pawn && Equals(pawn, enemyLateralPawn) && pawn.Position.Rank == (pawn.PieceOwner == Side.White ? 2 : 7)) {
							EnPassantMove testMove = new EnPassantMove(new Square(testSquare.Rank + (PieceOwner == Side.White ? 1 : -1)), this, enemyLateralPawn);

							if (Rules.MoveObeysRules(board, testMove, PieceOwner))
								ValidMoves.Add(new EnPassantMove(new Square(testSquare.Rank + (PieceOwner == Side.White ? 1 : -1)), this, enemyLateralPawn));
						}
					}
				}
			}
		}

		public override Piece Clone() {
			return new Pawn(this);
		}
	}
}