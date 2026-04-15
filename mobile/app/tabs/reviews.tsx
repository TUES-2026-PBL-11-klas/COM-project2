import ReviewsView from "../../src/views/reviews/ReviewsView";
import { useMentorReviews } from "../../src/contexts/MentorReviewsContext";
import { useMentorChat } from "../../src/contexts/MentorChatContext";

export default function ReviewsScreen() {
  const { selectedMentor } = useMentorReviews();
  const { selectedMentorForChat } = useMentorChat();

  // Show reviews for the mentor being chatted with, or the mentor selected for reviews
  const mentorToShow = selectedMentorForChat || selectedMentor;

  console.log('ReviewsScreen - selectedMentor:', selectedMentor?.name);
  console.log('ReviewsScreen - selectedMentorForChat:', selectedMentorForChat?.name);
  console.log('ReviewsScreen - mentorToShow:', mentorToShow?.name);

  return <ReviewsView mentor={mentorToShow} />;
}
